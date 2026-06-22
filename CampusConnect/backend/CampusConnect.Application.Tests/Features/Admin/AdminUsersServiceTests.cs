using CampusConnect.Application.Features.Admin;
using CampusConnect.Application.Common.Security;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Admin;

public class AdminUsersServiceTests
{
    [Fact]
    public async Task CreateUserAsync_CreatesUserWithSelectedCourseAndHashedPassword()
    {
        var course = new Course
        {
            Code = "TIF25A",
            StudyProgram = "Computer Science",
        };
        var users = new FakeUserRepository();
        var groups = new FakeGroupRepository();
        var service = new AdminUsersService(users, new FakeCourseRepository(course), groups);

        var result = await service.CreateUserAsync(new CreateAdminUserCommand(
            "Mara",
            "Muster",
            "mara.muster@dhbw-loerrach.de",
            "Student",
            "tif25a",
            "Start123!",
            true));

        Assert.True(result.IsSuccess);
        var createdUser = Assert.Single(users.Users);
        Assert.Equal("mara.muster@dhbw-loerrach.de", createdUser.Email);
        Assert.Equal("Mara Muster", createdUser.DisplayName);
        Assert.Equal("TIF25A", createdUser.Course);
        Assert.Equal("Computer Science", createdUser.StudyProgram);
        Assert.Equal(UserRole.Student, createdUser.Role);
        Assert.True(createdUser.IsActive);
        Assert.True(result.Value!.IsActive);
        Assert.True(PasswordHasher.Verify("Start123!", createdUser.PasswordHash));
        Assert.Contains("TIF25A", groups.SyncedCourseCodes);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsDuplicateEmail()
    {
        var existingUser = new User
        {
            DisplayName = "Mara Muster",
            Email = "mara.muster@dhbw-loerrach.de",
            Role = UserRole.Student
        };
        var course = new Course
        {
            Code = "TIF25A",
            StudyProgram = "Computer Science",
        };
        var service = new AdminUsersService(new FakeUserRepository(existingUser), new FakeCourseRepository(course), new FakeGroupRepository());

        var result = await service.CreateUserAsync(new CreateAdminUserCommand(
            "Mara",
            "Muster",
            "mara.muster@dhbw-loerrach.de",
            "Student",
            "TIF25A",
            "Start123!",
            true));

        Assert.False(result.IsSuccess);
        Assert.Equal("This email address is already registered.", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesProfileRoleAndCourse()
    {
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            Role = UserRole.Admin
        };
        var user = new User
        {
            DisplayName = "Vera",
            Email = "vera@dhbw-loerrach.de",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var sourceCourse = new Course
        {
            Code = "TIF25A",
            StudyProgram = "Computer Science",
        };
        var targetCourse = new Course
        {
            Code = "TIF25B",
            StudyProgram = "Computer Science",
        };
        var groups = new FakeGroupRepository();
        var service = new AdminUsersService(new FakeUserRepository(admin, user), new FakeCourseRepository(sourceCourse, targetCourse), groups);

        var result = await service.UpdateUserAsync(new UpdateAdminUserCommand(
            user.Id,
            "Vera Verwaltung",
            "vera.verwaltung@dhbw-loerrach.de",
            "Management",
            "TIF25B",
            false,
            admin.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Vera Verwaltung", user.DisplayName);
        Assert.Equal("vera.verwaltung@dhbw-loerrach.de", user.Email);
        Assert.Equal(UserRole.Management, user.Role);
        Assert.Equal("TIF25B", user.Course);
        Assert.False(user.IsActive);
        Assert.False(result.Value!.IsActive);
        Assert.Contains("TIF25A", groups.SyncedCourseCodes);
        Assert.Contains("TIF25B", groups.SyncedCourseCodes);
    }

    [Fact]
    public async Task UpdateUserAsync_PreventsCurrentAdminDemotingSelf()
    {
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            Course = "TIF25A",
            Role = UserRole.Admin
        };
        var course = new Course
        {
            Code = "TIF25A",
            StudyProgram = "Computer Science",
        };
        var service = new AdminUsersService(new FakeUserRepository(admin), new FakeCourseRepository(course), new FakeGroupRepository());

        var result = await service.UpdateUserAsync(new UpdateAdminUserCommand(
            admin.Id,
            "Admin",
            "admin@dhbw-loerrach.de",
            "Student",
            "TIF25A",
            true,
            admin.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("You cannot remove your own admin role.", result.Error);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task UpdateStatusAsync_DeactivatesAndReactivatesUser()
    {
        var user = new User
        {
            DisplayName = "Vera",
            Email = "vera@dhbw-loerrach.de",
            Role = UserRole.Student
        };
        var service = new AdminUsersService(new FakeUserRepository(user), new FakeCourseRepository(), new FakeGroupRepository());

        var deactivate = await service.UpdateStatusAsync(new UpdateUserStatusCommand(user.Id, false, Guid.NewGuid()));

        Assert.True(deactivate.IsSuccess);
        Assert.False(user.IsActive);
        Assert.False(deactivate.Value!.IsActive);

        var reactivate = await service.UpdateStatusAsync(new UpdateUserStatusCommand(user.Id, true, Guid.NewGuid()));

        Assert.True(reactivate.IsSuccess);
        Assert.True(user.IsActive);
        Assert.True(reactivate.Value!.IsActive);
    }

    [Fact]
    public async Task UpdateStatusAsync_PreventsCurrentAdminDeactivatingSelf()
    {
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            Role = UserRole.Admin
        };
        var service = new AdminUsersService(new FakeUserRepository(admin), new FakeCourseRepository(), new FakeGroupRepository());

        var result = await service.UpdateStatusAsync(new UpdateUserStatusCommand(admin.Id, false, admin.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("You cannot deactivate your own admin account.", result.Error);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task UpdateUserAsync_PreventsCurrentAdminDeactivatingSelf()
    {
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            Course = "TIF25A",
            Role = UserRole.Admin
        };
        var course = new Course
        {
            Code = "TIF25A",
            StudyProgram = "Computer Science",
        };
        var service = new AdminUsersService(new FakeUserRepository(admin), new FakeCourseRepository(course), new FakeGroupRepository());

        var result = await service.UpdateUserAsync(new UpdateAdminUserCommand(
            admin.Id,
            "Admin",
            "admin@dhbw-loerrach.de",
            "Admin",
            "TIF25A",
            false,
            admin.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("You cannot deactivate your own admin account.", result.Error);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task UpdateRoleAsync_AllowsAssigningManagementRole()
    {
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            Role = UserRole.Admin
        };
        var user = new User
        {
            DisplayName = "Vera",
            Email = "vera@dhbw-loerrach.de",
            Role = UserRole.Student
        };
        var users = new FakeUserRepository(admin, user);
        var service = new AdminUsersService(users, new FakeCourseRepository(), new FakeGroupRepository());

        var result = await service.UpdateRoleAsync(new UpdateUserRoleCommand(user.Id, "Management", admin.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Management", result.Value!.Role);
        Assert.Equal(UserRole.Management, user.Role);
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly List<User> _users = users.ToList();
        public IReadOnlyList<User> Users => _users;

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(_users);

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Email == email));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.RemoveAll(user => user.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCourseRepository(params Course[] courses) : ICourseRepository
    {
        private readonly List<Course> _courses = courses.ToList();

        public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Course>>(_courses);

        public Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_courses.FirstOrDefault(course => string.Equals(course.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Course course, CancellationToken cancellationToken = default)
        {
            _courses.Add(course);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroupRepository : IGroupRepository
    {
        private readonly List<string> _syncedCourseCodes = [];
        public IReadOnlyList<string> SyncedCourseCodes => _syncedCourseCodes;

        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>([]);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult<CampusGroup?>(null);

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null) => Task.FromResult(new CampusGroup());

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
            _syncedCourseCodes.Add(courseCode);
            return Task.CompletedTask;
        }
    }
}
