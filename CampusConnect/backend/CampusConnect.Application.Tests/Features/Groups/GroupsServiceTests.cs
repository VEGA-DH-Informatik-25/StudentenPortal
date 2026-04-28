using CampusConnect.Application.Features.Groups;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Groups;

public class GroupsServiceTests
{
    [Fact]
    public async Task GetGroupsForUserAsync_EnsuresCourseGroupFromProfile()
    {
        var user = new User
        {
            DisplayName = "Cara",
            Email = "cara@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 1,
            Course = "TIF26C"
        };
        var groups = new FakeGroupRepository();
        var service = new GroupsService(groups, new FakeUserRepository(user));

        var result = await service.GetGroupsForUserAsync(user.Id);

        Assert.Contains(result, group => group.Type == "Course" && group.CourseCode == "TIF26C");
    }

    [Fact]
    public async Task UpdateSettingsAsync_RejectsStudentChanges()
    {
        var user = new User
        {
            DisplayName = "Dina",
            Email = "dina@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 4,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = CourseGroup("TIF25A");
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.UpdateSettingsAsync(group.Id, user.Id, new UpdateGroupSettingsCommand(false, false, true, true));

        Assert.False(result.IsSuccess);
        Assert.Equal("Keine Berechtigung zum Bearbeiten dieser Gruppeneinstellungen.", result.Error);
    }

    [Fact]
    public async Task CreateGroupAsync_CreatesSocialGroupOwnedByUser()
    {
        var user = new User
        {
            DisplayName = "Eva",
            Email = "eva@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var groups = new FakeGroupRepository();
        var service = new GroupsService(groups, new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(user.Id, "Lerngruppe Web", "Gemeinsame Vorbereitung", "Interessierte Studierende"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Social", result.Value!.Type);
        Assert.Equal(user.Id, result.Value.OwnerUserId);
        Assert.True(result.Value.CanManage);
        Assert.Equal(1, result.Value.AssignedUserCount);
    }

    [Fact]
    public async Task GetSettingsDetailsAsync_RejectsUnownedSocialGroup()
    {
        var user = new User
        {
            DisplayName = "Finn",
            Email = "finn@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 3,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(Guid.NewGuid());
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.GetSettingsDetailsAsync(group.Id, user.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(GroupsService.PermissionError, result.Error);
    }

    [Fact]
    public async Task UpdateAssignmentsAsync_AssignsExistingAccountsAndKeepsOwner()
    {
        var owner = new User
        {
            DisplayName = "Gina",
            Email = "gina@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Hannes",
            Email = "hannes@dhbw-loerrach.de",
            StudyProgram = "Wirtschaftsinformatik",
            Semester = 2,
            Course = "WWI25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        var groups = new FakeGroupRepository(group);
        var service = new GroupsService(groups, new FakeUserRepository(owner, member));

        var result = await service.UpdateAssignmentsAsync(group.Id, owner.Id, new UpdateGroupAssignmentsCommand([member.Id]));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Accounts, account => account.Id == owner.Id && account.IsAssigned);
        Assert.Contains(result.Value.Accounts, account => account.Id == member.Id && account.IsAssigned);
        Assert.Equal(2, result.Value.Group.AssignedUserCount);
    }

    private static CampusGroup CourseGroup(string courseCode) => new()
    {
        Name = $"Kurs {courseCode}",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = "Informatik",
        IconLabel = "TI",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
    };

    private static CampusGroup SocialGroup(Guid ownerId) => new()
    {
        Name = "Lerngruppe Web",
        Description = "Gemeinsame Vorbereitung",
        Type = GroupType.Social,
        Audience = "Interessierte Studierende",
        OwnerUserId = ownerId,
        OwnerLabel = "Community",
        IconLabel = "LW",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true },
        AssignedUserIds = [ownerId]
    };

    private sealed class FakeGroupRepository(params CampusGroup[] groups) : IGroupRepository
    {
        private readonly List<CampusGroup> _groups = groups.ToList();

        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>(_groups);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult(_groups.FirstOrDefault(group => group.Id == id));

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
        {
            var existing = _groups.FirstOrDefault(group => group.CourseCode == courseCode);
            if (existing is not null)
                return Task.FromResult(existing);

            var group = CourseGroup(courseCode);
            _groups.Add(group);
            return Task.FromResult(group);
        }

        public Task AddAsync(CampusGroup group)
        {
            _groups.Add(group);
            return Task.CompletedTask;
        }

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings)
        {
            var group = _groups.First(group => group.Id == id);
            group.Settings = settings;
            return Task.CompletedTask;
        }

        public Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds)
        {
            var group = _groups.First(group => group.Id == id);
            group.AssignedUserIds = assignedUserIds.ToHashSet();
            return Task.CompletedTask;
        }
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
}
