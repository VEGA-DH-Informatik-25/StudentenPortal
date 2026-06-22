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
                Assert.Contains(courses, course => course.Code == "TIF25A" && course.StudyProgram == "Computer Science");
                Assert.Contains(courses, course => course.Code == "WDB25A" && course.StudyProgram == "Business Administration - Digital Business Management");
                Assert.Contains(courses, course => course.Code == "GIG25A" && course.StudyProgram == "Interprofessional Health Care");
                Assert.Contains(courses, course => course.Code == "LECTURER" && course.StudyProgram == "Lehrende");
                Assert.Contains(courses, course => course.Code == "MANAGEMENT" && course.StudyProgram == "Verwaltung");

                var users = await dbContext.Users.AsNoTracking().ToListAsync();
                Assert.Contains(users, user => user.Email == "demo.admin@dhbw-loerrach.de" && user.Role == UserRole.Admin && user.Course == "ADMIN");
                Assert.Contains(users, user => user.Email == "demo.technik@dhbw-loerrach.de" && user.Role == UserRole.Lecturer && user.Course == "LECTURER");
                var tifStudent = Assert.Single(users, user => user.Email == "lena.tif25a@dhbw-loerrach.de");
                var housingOwner = Assert.Single(users, user => user.Email == "noah.wwi25a@dhbw-loerrach.de");
                Assert.Equal("TIF25A", tifStudent.Course);

                var seededGroups = await groups.GetAllAsync();
                Assert.Contains(seededGroups, group => group.Name == "Exam office and deadlines" && group.Type == GroupType.Official);
                Assert.Contains(seededGroups, group => group.CourseCode == "TIF25A" && group.AssignedUserIds.Contains(tifStudent.Id));
                Assert.Contains(seededGroups, group =>
                    group.Name == "Housing in Loerrach" &&
                    group.Settings.IsDiscoverable &&
                    !group.Settings.RequiresApproval &&
                    group.AssignedUserIds.Contains(housingOwner.Id) &&
                    !group.AssignedUserIds.Contains(tifStudent.Id));

                var posts = await feed.GetAllAsync(1, 20);
                Assert.Contains(posts, post => post.Content.Contains("CampusConnect demo area", StringComparison.Ordinal));

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
        new() { Code = "TIF25A", StudyProgram = "Computer Science" },
        new() { Code = "WWI25A", StudyProgram = "Business Informatics" },
        new() { Code = "WDB25A", StudyProgram = "Business Administration - Digital Business Management" },
        new() { Code = "TMB25A", StudyProgram = "Mechanical Engineering" },
        new() { Code = "WGM24A", StudyProgram = "Business Health Management" },
        new() { Code = "GIG25A", StudyProgram = "Interprofessional Health Care" }
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
