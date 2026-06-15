using CampusConnect.Infrastructure.Persistence;
using CampusConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
            Assert.Null(adminCourse.Semester);

            Assert.Contains(await dbContext.Courses.AsNoTracking().ToListAsync(), course =>
                course.Code == "LECTURER" &&
                course.StudyProgram == "Lehrende" &&
                course.Semester is null &&
                course.IsActive);
            Assert.Contains(await dbContext.Courses.AsNoTracking().ToListAsync(), course =>
                course.Code == "MANAGEMENT" &&
                course.StudyProgram == "Verwaltung" &&
                course.Semester is null &&
                course.IsActive);
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

    [Fact]
    public async Task FeedModerationMigration_ShouldPublishExistingPostsAndAllowComments()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-migration-tests-{Guid.NewGuid():N}.db");
        var postId = Guid.NewGuid();

        try
        {
            await using (var oldContext = new CampusConnectDbContext(
                new DbContextOptionsBuilder<CampusConnectDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options))
            {
                await oldContext.GetService<IMigrator>().MigrateAsync("20260608083126_AddGroupTypeRenameAndJoinWorkflow");
                await oldContext.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO FeedPosts (Id, AuthorId, GroupId, AuthorName, Content, CreatedAt, Comments, Reactions)
                    VALUES ({postId}, {Guid.NewGuid()}, {Guid.NewGuid()}, {"Alice"}, {"Existing post"}, {DateTime.UtcNow}, {"[]"}, {"[]"})
                    """);
                await oldContext.GetService<IMigrator>().MigrateAsync();
            }

            await using var currentContext = new CampusConnectDbContext(
                new DbContextOptionsBuilder<CampusConnectDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options);
            var post = await currentContext.FeedPosts.AsNoTracking().SingleAsync(item => item.Id == postId);

            Assert.Equal(FeedPostStatus.Published, post.Status);
            Assert.True(post.AllowComments);
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
