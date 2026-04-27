using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ReturnsProfileFromRegistrationData()
    {
        var users = new FakeUserRepository();
        var service = new AuthService(users, new FakeJwtService());

        var result = await service.RegisterAsync(new RegisterCommand(
            "alice@dhbw-loerrach.de",
            "secret",
            "Alice",
            "Informatik",
            3,
            "TIF25A"));

        Assert.True(result.IsSuccess);
        var auth = result.Value!;
        Assert.Equal("test-token", auth.Token);
        Assert.Equal("alice@dhbw-loerrach.de", auth.Profile.Email);
        Assert.Equal("Alice", auth.Profile.DisplayName);
        Assert.Equal("Informatik", auth.Profile.StudyProgram);
        Assert.Equal(3, auth.Profile.Semester);
        Assert.Equal("TIF25A", auth.Profile.Course);

        var storedUser = await users.FindByEmailAsync("alice@dhbw-loerrach.de");
        Assert.NotNull(storedUser);
        Assert.Equal(auth.Profile.Id, storedUser.Id);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesOnlyProfileFieldsForUser()
    {
        var users = new FakeUserRepository();
        var user = new User
        {
            Email = "bob@dhbw-loerrach.de",
            PasswordHash = "hash",
            DisplayName = "Bob",
            StudyProgram = "Wirtschaftsinformatik",
            Semester = 1,
            Course = "WWI25A"
        };
        await users.AddAsync(user);
        var service = new AuthService(users, new FakeJwtService());

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand(
            "Bob B.",
            "Informatik",
            2,
            "TIF25B"));

        Assert.True(result.IsSuccess);
        var profile = result.Value!;
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("bob@dhbw-loerrach.de", profile.Email);
        Assert.Equal("Bob B.", profile.DisplayName);
        Assert.Equal("Informatik", profile.StudyProgram);
        Assert.Equal(2, profile.Semester);
        Assert.Equal("TIF25B", profile.Course);

        var storedUser = await users.FindByIdAsync(user.Id);
        Assert.Equal("hash", storedUser!.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfileAsync_RejectsInvalidProfileData()
    {
        var users = new FakeUserRepository();
        var user = new User
        {
            Email = "chris@dhbw-loerrach.de",
            PasswordHash = "hash",
            DisplayName = "Chris",
            StudyProgram = "Informatik",
            Semester = 4,
            Course = "TIF24A"
        };
        await users.AddAsync(user);
        var service = new AuthService(users, new FakeJwtService());

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand("", "Informatik", 7, "TIF24A"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Bitte fülle alle Profilfelder aus.", result.Error);
        var storedUser = await users.FindByIdAsync(user.Id);
        Assert.Equal("Chris", storedUser!.DisplayName);
        Assert.Equal(4, storedUser.Semester);
    }

    private sealed class FakeJwtService : IJwtService
    {
        public string GenerateToken(User user) => "test-token";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = [];

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(_users.Values.OrderBy(user => user.DisplayName).ToList());

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Values.FirstOrDefault(user => user.Email == email.ToLowerInvariant()));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.Remove(id);
            return Task.CompletedTask;
        }
    }
}