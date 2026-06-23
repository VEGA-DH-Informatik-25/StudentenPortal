using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_AcceptsEmailWithDifferentCaseAndWhitespace()
    {
        var users = new FakeUserRepository();
        var service = CreateService(users);
        await users.AddAsync(new User
        {
            Email = "alice@dhbw-loerrach.de",
            PasswordHash = CampusConnect.Application.Common.Security.PasswordHasher.Hash("secret"),
            DisplayName = "Alice",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            IsActive = true
        });

        var result = await service.LoginAsync(new LoginCommand("  ALICE@DHBW-LOERRACH.DE  ", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal("alice@dhbw-loerrach.de", result.Value!.Profile.Email);
    }

    [Fact]
    public async Task LoginAsync_RejectsInactiveUser()
    {
        var users = new FakeUserRepository();
        var service = CreateService(users);
        await users.AddAsync(new User
        {
            Email = "inactive@dhbw-loerrach.de",
            PasswordHash = CampusConnect.Application.Common.Security.PasswordHasher.Hash("secret"),
            DisplayName = "Inactive User",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            IsActive = false
        });

        var result = await service.LoginAsync(new LoginCommand("inactive@dhbw-loerrach.de", "secret"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email address or password.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_UsesNeutralErrorForUnknownAccountAndWrongPassword()
    {
        var users = new FakeUserRepository();
        var limiter = new FakeLoginRateLimiter();
        var service = CreateService(users, limiter: limiter);
        await users.AddAsync(new User
        {
            Email = "alice@dhbw-loerrach.de",
            PasswordHash = CampusConnect.Application.Common.Security.PasswordHasher.Hash("secret"),
            DisplayName = "Alice",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            IsActive = true
        });

        var unknownAccount = await service.LoginAsync(new LoginCommand("missing@dhbw-loerrach.de", "wrong", "127.0.0.1", "browser"));
        var wrongPassword = await service.LoginAsync(new LoginCommand("alice@dhbw-loerrach.de", "wrong", "127.0.0.1", "browser"));

        Assert.False(unknownAccount.IsSuccess);
        Assert.False(wrongPassword.IsSuccess);
        Assert.Equal("Invalid email address or password.", unknownAccount.Error);
        Assert.Equal("Invalid email address or password.", wrongPassword.Error);
        Assert.Equal(2, limiter.FailureCount);
    }

    [Fact]
    public async Task LoginAsync_ReturnsRateLimitErrorWhenLimiterBlocks()
    {
        var users = new FakeUserRepository();
        var limiter = new FakeLoginRateLimiter { IsLimitedOnCheck = true };
        var service = CreateService(users, limiter: limiter);

        var result = await service.LoginAsync(new LoginCommand("alice@dhbw-loerrach.de", "secret", "127.0.0.1", "browser"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthService.LoginRateLimitExceededError, result.Error);
        Assert.Equal(0, limiter.FailureCount);
    }

    [Fact]
    public async Task LoginAsync_ResetsRateLimiterAfterSuccessfulLogin()
    {
        var users = new FakeUserRepository();
        var limiter = new FakeLoginRateLimiter();
        var service = CreateService(users, limiter: limiter);
        await users.AddAsync(new User
        {
            Email = "alice@dhbw-loerrach.de",
            PasswordHash = CampusConnect.Application.Common.Security.PasswordHasher.Hash("secret"),
            DisplayName = "Alice",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            IsActive = true
        });

        var result = await service.LoginAsync(new LoginCommand("  ALICE@DHBW-LOERRACH.DE  ", "secret", "127.0.0.1", "browser"));

        Assert.True(result.IsSuccess);
        Assert.Single(limiter.ResetContexts);
        Assert.Equal("alice@dhbw-loerrach.de", limiter.ResetContexts[0].Account);
        Assert.Equal("127.0.0.1", limiter.ResetContexts[0].IpAddress);
        Assert.Equal("browser", limiter.ResetContexts[0].Device);
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
            StudyProgram = "Business Informatics",
            Course = "WWI25A"
        };
        await users.AddAsync(user);
        var groups = new FakeGroupRepository();
        var service = CreateService(users, groups: groups);

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand(
            "Bob B.",
            "TIF25B",
            " +49 7621 123456 ",
            " Library ",
            " Looking for a project partner for web development. "));

        Assert.True(result.IsSuccess);
        var profile = result.Value!;
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("bob@dhbw-loerrach.de", profile.Email);
        Assert.Equal("Bob B.", profile.DisplayName);
        Assert.Equal("Computer Science", profile.StudyProgram);
        Assert.Equal("TIF25B", profile.Course);
        Assert.Equal("+49 7621 123456", profile.PhoneNumber);
        Assert.Equal("Library", profile.Location);
        Assert.Equal("Looking for a project partner for web development.", profile.ProfileNote);
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
            StudyProgram = "Computer Science",
            Course = "TIF24A"
        };
        await users.AddAsync(user);
        var service = CreateService(users);

        var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileCommand("", "TIF25A", "", "", ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("Fill in all profile fields.", result.Error);
        var storedUser = await users.FindByIdAsync(user.Id);
        Assert.Equal("Chris", storedUser!.DisplayName);
    }

    private static AuthService CreateService(
        FakeUserRepository users,
        FakeCourseRepository? courses = null,
        FakeGroupRepository? groups = null,
        ILoginRateLimiter? limiter = null) =>
        new(users, new FakeJwtService(), courses ?? new FakeCourseRepository(), groups ?? new FakeGroupRepository(), limiter ?? new NoopLoginRateLimiter());

    private sealed class FakeJwtService : IJwtService
    {
        public string GenerateToken(User user) => "test-token";
    }

    private sealed class NoopLoginRateLimiter : ILoginRateLimiter
    {
        public LoginRateLimitResult CheckAndEscalateIfLimited(LoginRateLimitContext context) => new(false);

        public LoginRateLimitResult RegisterFailedAttempt(LoginRateLimitContext context) => new(false);

        public void Reset(LoginRateLimitContext context)
        {
        }
    }

    private sealed class FakeLoginRateLimiter : ILoginRateLimiter
    {
        public bool IsLimitedOnCheck { get; init; }
        public int FailureCount { get; private set; }
        public List<LoginRateLimitContext> ResetContexts { get; } = [];

        public LoginRateLimitResult CheckAndEscalateIfLimited(LoginRateLimitContext context) =>
            new(IsLimitedOnCheck);

        public LoginRateLimitResult RegisterFailedAttempt(LoginRateLimitContext context)
        {
            FailureCount++;
            return new(false);
        }

        public void Reset(LoginRateLimitContext context) =>
            ResetContexts.Add(context);
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
            ["TIF25A"] = new Course { Code = "TIF25A", StudyProgram = "Computer Science" },
            ["TIF25B"] = new Course { Code = "TIF25B", StudyProgram = "Computer Science" },
            ["WWI25A"] = new Course { Code = "WWI25A", StudyProgram = "Business Informatics" }
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

        public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds) => Task.CompletedTask;

        public Task RemoveMemberAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role) => Task.CompletedTask;

        public Task AddJoinRequestAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task RemoveJoinRequestAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds) => Task.CompletedTask;

        public Task RemoveInvitationAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
        {
            AssignedUserIdsByCourse[courseCode.Trim().ToUpperInvariant()] = assignedUserIds.ToHashSet();
            return Task.CompletedTask;
        }
    }
}
