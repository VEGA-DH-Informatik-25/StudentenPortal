using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusConnect.API.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "test.admin@dhbw-loerrach.de";
    public const string AdminPassword = "Admin123!";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-api-tests-{Guid.NewGuid():N}.db");
    private readonly string _dataProtectionPath = Path.Combine(Path.GetTempPath(), $"campusconnect-api-tests-keys-{Guid.NewGuid():N}");
    private readonly string _uploadPath = Path.Combine(Path.GetTempPath(), $"campusconnect-api-tests-uploads-{Guid.NewGuid():N}");

    public TestApiFactory()
    {
        SetTestConfiguration();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(_dataProtectionPath))
                .SetApplicationName("CampusConnect.API.Tests");
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CampusConnect"] = $"Data Source={_databasePath}",
                ["Jwt:Secret"] = TestJwt.Secret,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
                ["Admin:Email"] = AdminEmail,
                ["Admin:Password"] = AdminPassword,
                ["Admin:DisplayName"] = "Test Admin",
                ["Admin:Course"] = "TIF25A",
                ["Admin:StudyProgram"] = "Computer Science",
                ["Mensa:ApiKey"] = "test-key",
                ["Mensa:BaseUrl"] = "https://example.invalid",
                ["Mensa:LocationId"] = "677",
                ["Mensa:Days"] = "5",
                ["FeedAttachments:UploadPath"] = _uploadPath
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
            Directory.Delete(_dataProtectionPath, recursive: true);
            Directory.Delete(_uploadPath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void SetTestConfiguration()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CampusConnect", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwt.Secret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Admin__Email", AdminEmail);
        Environment.SetEnvironmentVariable("Admin__Password", AdminPassword);
        Environment.SetEnvironmentVariable("Admin__DisplayName", "Test Admin");
        Environment.SetEnvironmentVariable("Admin__Course", "TIF25A");
        Environment.SetEnvironmentVariable("Admin__StudyProgram", "Computer Science");
        Environment.SetEnvironmentVariable("Mensa__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("Mensa__BaseUrl", "https://example.invalid");
        Environment.SetEnvironmentVariable("Mensa__LocationId", "677");
        Environment.SetEnvironmentVariable("Mensa__Days", "5");
        Environment.SetEnvironmentVariable("FeedAttachments__UploadPath", _uploadPath);
    }

    private static void ClearTestConfiguration()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CampusConnect", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Admin__Email", null);
        Environment.SetEnvironmentVariable("Admin__Password", null);
        Environment.SetEnvironmentVariable("Admin__DisplayName", null);
        Environment.SetEnvironmentVariable("Admin__Course", null);
        Environment.SetEnvironmentVariable("Admin__StudyProgram", null);
        Environment.SetEnvironmentVariable("Mensa__ApiKey", null);
        Environment.SetEnvironmentVariable("Mensa__BaseUrl", null);
        Environment.SetEnvironmentVariable("Mensa__LocationId", null);
        Environment.SetEnvironmentVariable("Mensa__Days", null);
        Environment.SetEnvironmentVariable("FeedAttachments__UploadPath", null);
    }
}
