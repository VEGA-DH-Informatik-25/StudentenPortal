using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusConnect.API.Tests;

public sealed class AdminUsersApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task AdminCanCreateAndUpdateUser()
    {
        var client = await factory.CreateAdminClientAsync();

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
        var setupClient = await factory.CreateAdminClientAsync();
        var admin = await setupClient.CreateUserAsync("self-admin", role: "Admin", courseCode: "ADMIN");
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = admin.Email,
            password = admin.Password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var statusResponse = await client.PatchAsJsonAsync($"/api/admin/users/{admin.Id}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.BadRequest, statusResponse.StatusCode);
    }

    [Fact]
    public async Task AdminCanResetPasswordAndUserMustChangeItOnNextLogin()
    {
        var adminClient = await factory.CreateAdminClientAsync();
        var user = await adminClient.CreateUserAsync("password-reset");
        var newPassword = "ResetStart123!";

        var resetResponse = await adminClient.PatchAsJsonAsync($"/api/admin/users/{user.Id}/password", new
        {
            initialPassword = newPassword
        });

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var client = factory.CreateClient();
        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = user.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var profile = loginBody.GetProperty("profile");
        Assert.True(profile.GetProperty("mustChangePassword").GetBoolean());
        Assert.False(profile.GetProperty("onboardingCompleted").GetBoolean());
    }
}
