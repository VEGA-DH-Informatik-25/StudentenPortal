using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ReturnsProfileFromRegistrationData()
    {
        var users = new FakeUserRepository();
        var groups = new FakeGroupRepository();
        var service = CreateService(users, groups: groups);

        var result = await service.RegisterAsync(new RegisterCommand(
            "alice@dhbw-loerrach.de",
            "secret",
            "Alice",
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
        Assert.Contains(auth.Profile.Id, groups.AssignedUserIdsByCourse["TIF25A"]);
    }

    [Fact]
    public async Task RegisterAsync_RejectsUnknownCourse()
    {
        var service = CreateService(new FakeUserRepository());

        var result = await service.RegisterAsync(new RegisterCommand(
            "alice@dhbw-loerrach.de",
            "secret",
            "Alice",
            "UNKNOWN"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Bitte wähle einen gültigen Kurs aus.", result.Error);
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
        var groups = new FakeGroupRepository();
        var service = CreateService(users, groups: groups);

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand(
            "Bob B.",
            "TIF25B"));

        Assert.True(result.IsSuccess);
        var profile = result.Value!;
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("bob@dhbw-loerrach.de", profile.Email);
        Assert.Equal("Bob B.", profile.DisplayName);
        Assert.Equal("Informatik", profile.StudyProgram);
        Assert.Equal(3, profile.Semester);
        Assert.Equal("TIF25B", profile.Course);
        Assert.Contains(user.Id, groups.AssignedUserIdsByCourse["TIF25B"]);

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
        var service = CreateService(users);

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand("", "TIF25A"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Bitte fülle alle Profilfelder aus.", result.Error);
        var storedUser = await users.FindByIdAsync(user.Id);
        Assert.Equal("Chris", storedUser!.DisplayName);
        Assert.Equal(4, storedUser.Semester);
    }

    private static AuthService CreateService(
        FakeUserRepository users,
        FakeCourseRepository? courses = null,
        FakeGroupRepository? groups = null) =>
        new(users, new FakeJwtService(), courses ?? new FakeCourseRepository(), groups ?? new FakeGroupRepository());

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

    private sealed class FakeCourseRepository : ICourseRepository
    {
        private readonly Dictionary<string, Course> _courses = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TIF25A"] = new Course { Code = "TIF25A", StudyProgram = "Informatik", Semester = 3 },
            ["TIF25B"] = new Course { Code = "TIF25B", StudyProgram = "Informatik", Semester = 3 },
            ["WWI25A"] = new Course { Code = "WWI25A", StudyProgram = "Wirtschaftsinformatik", Semester = 1 }
        };

        public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Course>>(_courses.Values.ToList());

        public Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _courses.TryGetValue(code, out var course);
            return Task.FromResult(course);
        }

        public Task AddAsync(Course course, CancellationToken cancellationToken = default)
        {
            _courses[course.Code] = course;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroupRepository : IGroupRepository
    {
        public Dictionary<string, HashSet<Guid>> AssignedUserIdsByCourse { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<CampusGroup>>([]);

        public Task<CampusGroup?> FindByIdAsync(Guid id) =>
            Task.FromResult<CampusGroup?>(null);

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
        {
            var code = courseCode.Trim().ToUpperInvariant();
            AssignedUserIdsByCourse.TryAdd(code, []);
            return Task.FromResult(new CampusGroup
            {
                CourseCode = code,
                Name = code,
                Type = GroupType.Course,
                Description = studyProgram ?? string.Empty
            });
        }

        public Task AddAsync(CampusGroup group) => Task.CompletedTask;

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings) => Task.CompletedTask;

        public Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds) => Task.CompletedTask;

        public Task UpdateMemberPermissionsAsync(Guid id, IReadOnlyDictionary<Guid, GroupMemberPermission> permissions) => Task.CompletedTask;

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
        {
            AssignedUserIdsByCourse[courseCode.Trim().ToUpperInvariant()] = assignedUserIds.ToHashSet();
            return Task.CompletedTask;
        }
    }
}
