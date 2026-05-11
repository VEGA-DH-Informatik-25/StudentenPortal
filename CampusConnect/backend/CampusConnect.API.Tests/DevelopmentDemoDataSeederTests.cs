using CampusConnect.Domain.Enums;
using CampusConnect.Infrastructure.Persistence;
using CampusConnect.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusConnect.API.Tests;

public sealed class DevelopmentDemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenEnabled_ShouldPopulateDevelopmentHubData()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-demo-tests-{Guid.NewGuid():N}.db");

        try
        {
            await using (var dbContext = CreateDbContext(databasePath))
            {
                await dbContext.Database.MigrateAsync();

                var groups = new EntityGroupRepository(dbContext);
                var feed = new EntityFeedRepository(dbContext);
                var grades = new EntityGradeRepository(dbContext);
                var exams = new EntityExamRepository(dbContext);
                var seeder = new DevelopmentDemoDataSeeder(
                    dbContext,
                    Options.Create(new DemoDataOptions
                    {
                        Enabled = true,
                        Password = "TestDemoPass123!",
                        TechnicalCoursePrefixes = ["T", "WWI"],
                        Courses = DemoCourses()
                    }),
                    groups,
                    feed,
                    grades,
                    exams);

                await seeder.SeedAsync();

                var courses = await dbContext.Courses.AsNoTracking().ToListAsync();
                Assert.Contains(courses, course => course.Code == "TIF25A" && course.StudyProgram == "Informatik");
                Assert.Contains(courses, course => course.Code == "WDB25A" && course.StudyProgram == "BWL-Digital Business Management");
                Assert.Contains(courses, course => course.Code == "GIG25A" && course.StudyProgram == "Interprofessionelle Gesundheitsversorgung");

                var users = await dbContext.Users.AsNoTracking().ToListAsync();
                Assert.Contains(users, user => user.Email == "demo.admin@dhbw-loerrach.de" && user.Role == UserRole.Admin);
                var tifStudent = Assert.Single(users, user => user.Email == "lena.tif25a@dhbw-loerrach.de");
                var housingOwner = Assert.Single(users, user => user.Email == "noah.wwi25a@dhbw-loerrach.de");
                Assert.Equal("TIF25A", tifStudent.Course);

                var seededGroups = await groups.GetAllAsync();
                Assert.Contains(seededGroups, group => group.Name == "Prüfungsamt und Fristen" && group.Type == GroupType.Official);
                Assert.Contains(seededGroups, group => group.CourseCode == "TIF25A" && group.AssignedUserIds.Contains(tifStudent.Id));
                Assert.Contains(seededGroups, group =>
                    group.Name == "Wohnungssuche Lörrach" &&
                    group.Settings.IsDiscoverable &&
                    !group.Settings.RequiresApproval &&
                    group.AssignedUserIds.Contains(housingOwner.Id) &&
                    !group.AssignedUserIds.Contains(tifStudent.Id));

                var posts = await feed.GetAllAsync(1, 20);
                Assert.Contains(posts, post => post.Content.Contains("CampusConnect-Demobereich", StringComparison.Ordinal));

                Assert.NotEmpty(await grades.GetByUserAsync(tifStudent.Id));
                Assert.NotEmpty(await exams.GetByUserAsync(tifStudent.Id));
            }
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-wal");
            DeleteIfExists($"{databasePath}-shm");
        }
    }

    private static List<DemoCourseOptions> DemoCourses() =>
    [
        new() { Code = "TIF25A", StudyProgram = "Informatik", Semester = 2 },
        new() { Code = "WWI25A", StudyProgram = "Wirtschaftsinformatik", Semester = 2 },
        new() { Code = "WDB25A", StudyProgram = "BWL-Digital Business Management", Semester = 2 },
        new() { Code = "TMB25A", StudyProgram = "Maschinenbau", Semester = 2 },
        new() { Code = "WGM24A", StudyProgram = "BWL-Gesundheitsmanagement", Semester = 4 },
        new() { Code = "GIG25A", StudyProgram = "Interprofessionelle Gesundheitsversorgung", Semester = 2 }
    ];

    private static CampusConnectDbContext CreateDbContext(string databasePath) => new(
        new DbContextOptionsBuilder<CampusConnectDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options);

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