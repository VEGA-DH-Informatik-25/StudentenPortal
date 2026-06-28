using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CampusConnect.API.Tests;

public sealed class GroupModerationApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task GroupModerationWorkflow_ShouldPublishPendingPostAndDeleteGroup()
    {
        var (ownerClient, _) = await factory.CreateAuthenticatedUserClientAsync("owner");
        var (memberClient, member) = await factory.CreateAuthenticatedUserClientAsync("member");

        var missingGroup = await ownerClient.GetAsync($"/api/groups/{Guid.NewGuid()}/pending-posts");
        Assert.Equal(HttpStatusCode.BadRequest, missingGroup.StatusCode);

        var createGroup = await ownerClient.PostAsJsonAsync("/api/groups", new
        {
            name = "Moderated study group",
            description = "Posts are reviewed before publication.",
            audience = "Students",
            type = "Campus",
            allowStudentPosts = true,
            allowComments = true,
            requiresApproval = true,
            isDiscoverable = true,
            joinRule = "Open"
        });
        Assert.Equal(HttpStatusCode.Created, createGroup.StatusCode);
        var groupId = await ReadGuidAsync(createGroup, "id");

        var addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { member.Id } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var createPost = await memberClient.PostAsJsonAsync("/api/feed", new
        {
            groupId,
            content = "Please approve this post.",
            allowComments = false
        });
        Assert.Equal(HttpStatusCode.Created, createPost.StatusCode);
        var createdPost = await createPost.Content.ReadFromJsonAsync<JsonElement>();
        var postId = createdPost.GetProperty("id").GetGuid();
        Assert.Equal("Pending", createdPost.GetProperty("status").GetString());

        var pending = await ownerClient.GetFromJsonAsync<JsonElement[]>($"/api/groups/{groupId}/pending-posts");
        Assert.NotNull(pending);
        Assert.Single(pending!);

        var approve = await ownerClient.PostAsync($"/api/feed/{postId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        Assert.Equal("Published", await ReadStringAsync(approve, "status"));

        var feed = await memberClient.GetFromJsonAsync<JsonElement[]>("/api/feed");
        Assert.NotNull(feed);
        Assert.Contains(feed!, post => post.GetProperty("id").GetGuid() == postId);

        var forbiddenDelete = await memberClient.DeleteAsync($"/api/groups/{groupId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDelete.StatusCode);

        var delete = await ownerClient.DeleteAsync($"/api/groups/{groupId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task LeaveGroup_ShouldRemoveMemberAndTransferOwner()
    {
        var (ownerClient, _) = await factory.CreateAuthenticatedUserClientAsync("leave-owner");
        var (memberClient, member) = await factory.CreateAuthenticatedUserClientAsync("leave-member");

        var createGroup = await ownerClient.PostAsJsonAsync("/api/groups", new
        {
            name = "Leave study group",
            description = "Members can leave the group.",
            audience = "Students",
            type = "Campus",
            allowStudentPosts = true,
            allowComments = true,
            requiresApproval = false,
            isDiscoverable = true,
            joinRule = "Open"
        });
        Assert.Equal(HttpStatusCode.Created, createGroup.StatusCode);
        var groupId = await ReadGuidAsync(createGroup, "id");

        var addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { member.Id } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var memberLeave = await memberClient.PostAsJsonAsync($"/api/groups/{groupId}/leave", new { });
        Assert.Equal(HttpStatusCode.OK, memberLeave.StatusCode);
        var memberLeaveBody = await memberLeave.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(memberLeaveBody.GetProperty("deleted").GetBoolean());

        addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { member.Id } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var ownerLeave = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/leave", new { newOwnerUserId = member.Id });
        Assert.Equal(HttpStatusCode.OK, ownerLeave.StatusCode);
        var ownerLeaveBody = await ownerLeave.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(ownerLeaveBody.GetProperty("deleted").GetBoolean());
        Assert.Equal(member.Id, ownerLeaveBody.GetProperty("group").GetProperty("ownerUserId").GetGuid());

        var formerOwnerGroups = await ownerClient.GetFromJsonAsync<JsonElement[]>("/api/groups");
        Assert.NotNull(formerOwnerGroups);
        Assert.Contains(formerOwnerGroups!, group => group.GetProperty("id").GetGuid() == groupId && !group.GetProperty("isAssigned").GetBoolean());

    }

    [Fact]
    public async Task FeedAttachments_ShouldUploadAndRequireGroupReadAccess()
    {
        var (ownerClient, _) = await factory.CreateAuthenticatedUserClientAsync("attachment-owner");
        var (memberClient, member) = await factory.CreateAuthenticatedUserClientAsync("attachment-member");
        var (outsiderClient, _) = await factory.CreateAuthenticatedUserClientAsync("attachment-outsider");

        var createGroup = await ownerClient.PostAsJsonAsync("/api/groups", new
        {
            name = "Attachment study group",
            description = "Files for the group.",
            audience = "Students",
            type = "Campus",
            allowStudentPosts = true,
            allowComments = true,
            requiresApproval = false,
            isDiscoverable = true,
            joinRule = "Open"
        });
        Assert.Equal(HttpStatusCode.Created, createGroup.StatusCode);
        var groupId = await ReadGuidAsync(createGroup, "id");

        var addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { member.Id } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(groupId.ToString()), "groupId");
        form.Add(new StringContent("Hallo mit Datei"), "content");
        form.Add(new StringContent("true"), "allowComments");
        form.Add(new StringContent("Hallo mit Datei"), "translations.de");
        form.Add(new StringContent("Hello with file"), "translations.en");
        form.Add(new StringContent("Bonjour avec fichier"), "translations.fr");
        var file = new ByteArrayContent("hello"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "attachments", "notice.pdf");

        var createPost = await ownerClient.PostAsync("/api/feed", form);
        Assert.Equal(HttpStatusCode.Created, createPost.StatusCode);
        var post = await createPost.Content.ReadFromJsonAsync<JsonElement>();
        var postId = post.GetProperty("id").GetGuid();
        Assert.Equal("Hello with file", post.GetProperty("translations").GetProperty("en").GetString());
        var attachment = post.GetProperty("attachments").EnumerateArray().Single();
        var attachmentId = attachment.GetProperty("id").GetGuid();
        Assert.Equal("notice.pdf", attachment.GetProperty("fileName").GetString());

        var download = await memberClient.GetAsync($"/api/feed/{postId}/attachments/{attachmentId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);

        var forbidden = await outsiderClient.GetAsync($"/api/feed/{postId}/attachments/{attachmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty(property).GetGuid();
    }

    private static async Task<string> ReadStringAsync(HttpResponseMessage response, string property)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty(property).GetString()!;
    }
}
