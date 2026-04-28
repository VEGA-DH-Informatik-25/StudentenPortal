using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CampusConnect.API.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-api-tests-{Guid.NewGuid():N}.db");

    public TestApiFactory()
    {
        SetTestConfiguration();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CampusConnect"] = $"Data Source={_databasePath}",
                ["Jwt:Secret"] = TestJwt.Secret,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
                ["Admin:Email"] = string.Empty,
                ["Admin:Password"] = string.Empty,
                ["Mensa:ApiKey"] = "test-key",
                ["Mensa:BaseUrl"] = "https://example.invalid",
                ["Mensa:OrtId"] = "677",
                ["Mensa:Days"] = "5"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ClearTestConfiguration();

        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
        }
    }

    private void SetTestConfiguration()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CampusConnect", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwt.Secret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Admin__Email", string.Empty);
        Environment.SetEnvironmentVariable("Admin__Password", string.Empty);
        Environment.SetEnvironmentVariable("Mensa__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("Mensa__BaseUrl", "https://example.invalid");
        Environment.SetEnvironmentVariable("Mensa__OrtId", "677");
        Environment.SetEnvironmentVariable("Mensa__Days", "5");
    }

    private static void ClearTestConfiguration()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CampusConnect", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Admin__Email", null);
        Environment.SetEnvironmentVariable("Admin__Password", null);
        Environment.SetEnvironmentVariable("Mensa__ApiKey", null);
        Environment.SetEnvironmentVariable("Mensa__BaseUrl", null);
        Environment.SetEnvironmentVariable("Mensa__OrtId", null);
        Environment.SetEnvironmentVariable("Mensa__Days", null);
    }
}