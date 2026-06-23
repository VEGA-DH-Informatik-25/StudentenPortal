using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusConnect.API.Tests;

internal sealed record TestUser(Guid Id, string Email, string Password);

internal static class TestApiUsers
{
    public static async Task<HttpClient> CreateAdminClientAsync(this TestApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    public static async Task<TestUser> CreateUserAsync(this HttpClient adminClient, string prefix, string role = "Student", string courseCode = "TIF25A", bool isActive = true)
    {
        var password = "Start123!";
        var email = $"{prefix}-{Guid.NewGuid():N}@dhbw-loerrach.de".ToLowerInvariant();
        var response = await adminClient.PostAsJsonAsync("/api/admin/users", new
        {
            firstName = prefix,
            lastName = "User",
            email,
            role,
            courseCode,
            initialPassword = password,
            isActive
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TestUser(body.GetProperty("id").GetGuid(), email, password);
    }

    public static async Task<(HttpClient Client, TestUser User)> CreateAuthenticatedUserClientAsync(this TestApiFactory factory, string prefix)
    {
        var adminClient = await factory.CreateAdminClientAsync();
        var user = await adminClient.CreateUserAsync(prefix);
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = user.Password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        return (client, user);
    }
}
