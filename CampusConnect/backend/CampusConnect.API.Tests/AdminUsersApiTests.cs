using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusConnect.API.Tests;

public sealed class AdminUsersApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task AdminCanCreateAndUpdateUser()
    {
        var client = factory.CreateClient();
        var adminId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(adminId, "Admin"));

        var createResponse = await client.PostAsJsonAsync("/api/admin/users", new
        {
            firstName = "Nora",
            lastName = "Neu",
            email = $"nora.neu-{Guid.NewGuid():N}@dhbw-loerrach.de",
            role = "Student",
            courseCode = "ADMIN",
            initialPassword = "Start123!",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = createdUser.GetProperty("id").GetGuid();
        Assert.Equal("Nora Neu", createdUser.GetProperty("displayName").GetString());
        Assert.Equal("Student", createdUser.GetProperty("role").GetString());
        Assert.True(createdUser.GetProperty("isActive").GetBoolean());

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/users/{userId}", new
        {
            displayName = "Nora Verwaltung",
            email = $"nora.verwaltung-{Guid.NewGuid():N}@dhbw-loerrach.de",
            role = "Management",
            courseCode = "ADMIN",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nora Verwaltung", updatedUser.GetProperty("displayName").GetString());
        Assert.Equal("Management", updatedUser.GetProperty("role").GetString());
        Assert.Equal("ADMIN", updatedUser.GetProperty("course").GetString());

        var statusResponse = await client.PatchAsJsonAsync($"/api/admin/users/{userId}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var deactivatedUser = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(deactivatedUser.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task AdminCannotDeactivateSelf()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(Guid.NewGuid(), "Admin"));

        var createResponse = await client.PostAsJsonAsync("/api/admin/users", new
        {
            firstName = "Selma",
            lastName = "Selfadmin",
            email = $"selma.selfadmin-{Guid.NewGuid():N}@dhbw-loerrach.de",
            role = "Admin",
            courseCode = "ADMIN",
            initialPassword = "Start123!",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var adminId = createdUser.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(adminId, "Admin"));

        var statusResponse = await client.PatchAsJsonAsync($"/api/admin/users/{adminId}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.BadRequest, statusResponse.StatusCode);
    }
}
