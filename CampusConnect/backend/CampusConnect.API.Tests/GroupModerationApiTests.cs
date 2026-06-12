using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusConnect.API.Tests;

public sealed class GroupModerationApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task GroupModerationWorkflow_ShouldPublishPendingPostAndDeleteGroup()
    {
        var ownerClient = factory.CreateClient();
        var memberClient = factory.CreateClient();
        var ownerId = await RegisterAsync(ownerClient, "owner");
        var memberId = await RegisterAsync(memberClient, "member");

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

        var addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { memberId } });
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
        var ownerClient = factory.CreateClient();
        var memberClient = factory.CreateClient();
        _ = await RegisterAsync(ownerClient, "leave-owner");
        var memberId = await RegisterAsync(memberClient, "leave-member");

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

        var addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { memberId } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var memberLeave = await memberClient.PostAsJsonAsync($"/api/groups/{groupId}/leave", new { });
        Assert.Equal(HttpStatusCode.OK, memberLeave.StatusCode);
        var memberLeaveBody = await memberLeave.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(memberLeaveBody.GetProperty("deleted").GetBoolean());

        addMember = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/members", new { userIds = new[] { memberId } });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var ownerLeave = await ownerClient.PostAsJsonAsync($"/api/groups/{groupId}/leave", new { newOwnerUserId = memberId });
        Assert.Equal(HttpStatusCode.OK, ownerLeave.StatusCode);
        var ownerLeaveBody = await ownerLeave.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(ownerLeaveBody.GetProperty("deleted").GetBoolean());
        Assert.Equal(memberId, ownerLeaveBody.GetProperty("group").GetProperty("ownerUserId").GetGuid());

        var formerOwnerGroups = await ownerClient.GetFromJsonAsync<JsonElement[]>("/api/groups");
        Assert.NotNull(formerOwnerGroups);
        Assert.Contains(formerOwnerGroups!, group => group.GetProperty("id").GetGuid() == groupId && !group.GetProperty("isAssigned").GetBoolean());

    }

    private static async Task<Guid> RegisterAsync(HttpClient client, string prefix)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{prefix}-{Guid.NewGuid():N}@dhbw-loerrach.de",
            password = "secret123",
            displayName = prefix,
            course = "ADMIN"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        return await ReadGuidAsync(profile, "id");
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
