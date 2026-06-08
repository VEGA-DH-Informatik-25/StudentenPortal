using CampusConnect.Application.Features.Admin;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Admin;

public class AdminUsersServiceTests
{
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

    private sealed class FakeCourseRepository : ICourseRepository
    {
        public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Course>>([]);

        public Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<Course?>(null);

        public Task AddAsync(Course course, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeGroupRepository : IGroupRepository
    {
        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>([]);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult<CampusGroup?>(null);

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null) => Task.FromResult(new CampusGroup());

        public Task AddAsync(CampusGroup group) => Task.CompletedTask;

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings) => Task.CompletedTask;

        public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds) => Task.CompletedTask;

        public Task RemoveMemberAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role) => Task.CompletedTask;

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds) => Task.CompletedTask;
    }
}
