using CampusConnect.Domain.Entities;
using CampusConnect.Infrastructure.Persistence;
using CampusConnect.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.API.Tests;

public sealed class EntityUserRepositoryTests
{
    [Fact]
    public async Task AddAndFindByEmailAsync_ShouldNormalizeEmailCaseAndWhitespace()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campusconnect-user-repo-tests-{Guid.NewGuid():N}.db");

        try
        {
            await using var dbContext = new CampusConnectDbContext(
                new DbContextOptionsBuilder<CampusConnectDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options);
            await dbContext.Database.EnsureCreatedAsync();

            var repository = new EntityUserRepository(dbContext);
            var user = new User
            {
                Email = "  Alice.Example@DHBW-Loerrach.de  ",
                PasswordHash = "hash",
                DisplayName = "Alice Example",
                StudyProgram = "Informatik",
                Semester = 2,
                Course = "TIF25A"
            };

            await repository.AddAsync(user);

            var storedUser = await dbContext.Users.AsNoTracking().SingleAsync();
            Assert.Equal("alice.example@dhbw-loerrach.de", storedUser.Email);

            var foundUser = await repository.FindByEmailAsync("ALICE.EXAMPLE@DHBW-LOERRACH.DE");
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser!.Id);
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
