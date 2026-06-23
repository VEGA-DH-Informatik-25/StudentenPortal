using System.Net;
using System.Net.Http.Json;

namespace CampusConnect.API.Tests;

public sealed class AuthLoginRateLimitApiTests
{
    [Fact]
    public async Task Login_FifthFailedAttempt_ReturnsTooManyRequestsWithNeutralMessage()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"CampusConnectRateLimitTest/{Guid.NewGuid():N}");

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = $"missing-{Guid.NewGuid():N}@dhbw-loerrach.de",
                password = "Wrong123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@dhbw-loerrach.de",
            password = "Wrong123!"
        });
        var body = await limitedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        Assert.Contains("Too many login attempts. Please try again later.", body);
        Assert.DoesNotContain("not found", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exists", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_SuccessfulAttemptAfterFailures_ResetsFailureCounters()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"CampusConnectRateLimitResetTest/{Guid.NewGuid():N}");

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = TestApiFactory.AdminEmail,
                password = "Wrong123!"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var successfulResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);

        var responseAfterReset = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = "Wrong123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, responseAfterReset.StatusCode);
    }
}
