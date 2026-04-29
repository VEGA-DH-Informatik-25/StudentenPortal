using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusConnect.API.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldEnsureConfiguredAdminCourseExistsAndIsActive()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-init-tests-{Guid.NewGuid():N}.db");

        try
        {
            await using var dbContext = new CampusConnectDbContext(
                new DbContextOptionsBuilder<CampusConnectDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options);

            var options = Options.Create(new AdminOptions
            {
                Email = "admin@dhbw-loerrach.de",
                Password = "Passw0rd!",
                Course = "admin",
                StudyProgram = "Administration",
                Semester = 1
            });

            var initializer = new DatabaseInitializer(dbContext, options);

            await initializer.InitializeAsync();

            var adminCourse = await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(course => course.Code == "ADMIN");
            Assert.NotNull(adminCourse);
            Assert.True(adminCourse!.IsActive);
            Assert.Equal("Administration", adminCourse.StudyProgram);
            Assert.Equal(1, adminCourse.Semester);
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-wal");
            DeleteIfExists($"{databasePath}-shm");
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldEnsureConfiguredAdminCourseEvenWithoutAdminCredentials()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-init-tests-{Guid.NewGuid():N}.db");

        try
        {
            await using var dbContext = new CampusConnectDbContext(
                new DbContextOptionsBuilder<CampusConnectDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options);

            var initializer = new DatabaseInitializer(dbContext, Options.Create(new AdminOptions
            {
                Email = string.Empty,
                Password = string.Empty,
                Course = "admin",
                StudyProgram = "Administration",
                Semester = 1
            }));

            await initializer.InitializeAsync();

            var adminCourse = await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(course => course.Code == "ADMIN");
            Assert.NotNull(adminCourse);
            Assert.True(adminCourse!.IsActive);
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-wal");
            DeleteIfExists($"{databasePath}-shm");
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
