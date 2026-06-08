using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Infrastructure.Persistence;
using CampusConnect.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.API.Tests;

public sealed class EntityFeatureRepositoryTests
{
    [Fact]
    public async Task FeatureRepositories_ShouldPersistDataAcrossDbContextInstances()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-feature-repo-tests-{Guid.NewGuid():N}.db");
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();

        try
        {
            await using (var dbContext = CreateDbContext(databasePath))
            {
                await dbContext.Database.MigrateAsync();

                var groups = new EntityGroupRepository(dbContext);
                var feed = new EntityFeedRepository(dbContext);
                var grades = new EntityGradeRepository(dbContext);
                var exams = new EntityExamRepository(dbContext);

                await groups.AddAsync(new CampusGroup
                {
                    Id = groupId,
                    Name = "Database study group",
                    Description = "Shared preparation for the database exam.",
                    Type = GroupType.Social,
                    Audience = "TIF25A",
                    OwnerUserId = userId,
                    OwnerLabel = "Alice",
                    IconLabel = "DB",
                    AccentColor = "#2563eb",
                    AssignedUserIds = [userId, moderatorId],
                    MemberRoles = new Dictionary<Guid, GroupRole> { [moderatorId] = GroupRole.Moderator }
                });

                await feed.AddAsync(new FeedPost
                {
                    Id = postId,
                    AuthorId = userId,
                    AuthorName = "Alice",
                    GroupId = groupId,
                    Content = "First meeting on Thursday.",
                    Comments =
                    [
                        new FeedComment { AuthorId = userId, AuthorName = "Alice", Content = "Room follows." }
                    ],
                    Reactions =
                    [
                        new FeedReaction { Emoji = "👍", UserIds = [userId] }
                    ]
                });

                await grades.AddAsync(new Grade
                {
                    UserId = userId,
                    ModuleName = "Databases",
                    ModuleCode = "T3INF2001",
                    Value = 1.7m,
                    Ects = 5
                });

                await exams.AddAsync(new ExamEntry
                {
                    UserId = userId,
                    ModuleName = "Databases",
                    ExamDate = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
                    Location = "Auditorium",
                    Notes = "Calculator allowed"
                });
            }

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var groups = new EntityGroupRepository(dbContext);
                var feed = new EntityFeedRepository(dbContext);
                var grades = new EntityGradeRepository(dbContext);
                var exams = new EntityExamRepository(dbContext);

                var persistedGroup = await groups.FindByIdAsync(groupId);
                Assert.NotNull(persistedGroup);
                Assert.Contains(userId, persistedGroup!.AssignedUserIds);
                Assert.Equal(GroupRole.Moderator, persistedGroup.MemberRoles[moderatorId]);

                var persistedPost = await feed.FindByIdAsync(postId);
                Assert.NotNull(persistedPost);
                Assert.Equal("First meeting on Thursday.", persistedPost!.Content);
                Assert.Single(persistedPost.Comments);
                Assert.Contains(userId, persistedPost.Reactions.Single().UserIds);

                var persistedGrades = await grades.GetByUserAsync(userId);
                var persistedGrade = Assert.Single(persistedGrades);
                Assert.Equal("Databases", persistedGrade.ModuleName);
                Assert.Equal(1.7m, persistedGrade.Value);

                var persistedExams = await exams.GetByUserAsync(userId);
                var persistedExam = Assert.Single(persistedExams);
                Assert.Equal("Auditorium", persistedExam.Location);
                Assert.Equal("Calculator allowed", persistedExam.Notes);
            }
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-wal");
            DeleteIfExists($"{databasePath}-shm");
        }
    }

    [Fact]
    public async Task EntityFeedRepository_ShouldReturnCloneInsteadOfTrackedPostReference()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-feed-repo-tests-{Guid.NewGuid():N}.db");

        try
        {
            await using var dbContext = CreateDbContext(databasePath);
            await dbContext.Database.MigrateAsync();
            var repository = new EntityFeedRepository(dbContext);
            var post = new FeedPost
            {
                AuthorId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                AuthorName = "Alice",
                Content = "Original"
            };

            await repository.AddAsync(post);
            var firstRead = await repository.FindByIdAsync(post.Id);
            Assert.NotNull(firstRead);

            firstRead!.Content = "Mutated outside repository";
            firstRead.Comments.Add(new FeedComment { AuthorId = Guid.NewGuid(), AuthorName = "Bob", Content = "Leaked" });

            var secondRead = await repository.FindByIdAsync(post.Id);

            Assert.NotNull(secondRead);
            Assert.Equal("Original", secondRead!.Content);
            Assert.Empty(secondRead.Comments);
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-wal");
            DeleteIfExists($"{databasePath}-shm");
        }
    }

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