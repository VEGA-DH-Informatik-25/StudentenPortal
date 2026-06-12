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
            StudyProgram = "Computer Science",
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
            StudyProgram = "Computer Science",
            Semester = 4,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = CourseGroup("TIF25A");
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.UpdateSettingsAsync(group.Id, user.Id, new UpdateGroupSettingsCommand(false, false, true, true));

        Assert.False(result.IsSuccess);
        Assert.Equal(GroupsService.PermissionError, result.Error);
    }

    [Fact]
    public async Task CreateGroupAsync_CreatesSocialGroupOwnedByUser()
    {
        var user = new User
        {
            DisplayName = "Eva",
            Email = "eva@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var groups = new FakeGroupRepository();
        var service = new GroupsService(groups, new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(user.Id, "Web study group", "Shared preparation", "Interested students"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Campus", result.Value!.Type);
        Assert.Equal(user.Id, result.Value.OwnerUserId);
        Assert.True(result.Value.CanManage);
        Assert.Equal(1, result.Value.AssignedUserCount);
    }

    [Theory]
    [InlineData("Official")]
    [InlineData("Course")]
    [InlineData("Campus")]
    public async Task CreateGroupAsync_AllowsManagementToCreateEveryGroupType(string type)
    {
        var user = new User
        {
            DisplayName = "Vera Management",
            Email = "vera@dhbw-loerrach.de",
            StudyProgram = "Campus Management",
            Semester = 1,
            Course = "ADM25A",
            Role = UserRole.Management
        };
        var groups = new FakeGroupRepository();
        var service = new GroupsService(groups, new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            $"{type} group",
            "Organizational group",
            "Campus",
            Type: type,
            CourseCode: type == "Course" ? "tif25a" : null,
            OfficialCategory: type == "Official" ? "Verwaltung" : null));

        Assert.True(result.IsSuccess);
        Assert.Equal(type, result.Value!.Type);
        Assert.Equal(user.Id, result.Value.OwnerUserId);
        Assert.True(result.Value.CanManage);
        if (type == "Course")
            Assert.Equal("TIF25A", result.Value.CourseCode);
    }

    [Theory]
    [InlineData("Official")]
    [InlineData("Course")]
    public async Task CreateGroupAsync_RejectsStudentForManagedGroupTypes(string type)
    {
        var user = new User
        {
            DisplayName = "Eva",
            Email = "eva@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var service = new GroupsService(new FakeGroupRepository(), new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            $"{type} group",
            "Organizational group",
            "Campus",
            Type: type,
            CourseCode: type == "Course" ? "TIF25A" : null));

        Assert.False(result.IsSuccess);
        Assert.Equal("This global role cannot create this group type.", result.Error);
    }

    [Theory]
    [InlineData(UserRole.Student, "Official", false)]
    [InlineData(UserRole.Student, "Course", false)]
    [InlineData(UserRole.Student, "Campus", true)]
    [InlineData(UserRole.Lecturer, "Official", false)]
    [InlineData(UserRole.Lecturer, "Course", true)]
    [InlineData(UserRole.Lecturer, "Campus", true)]
    [InlineData(UserRole.Management, "Official", true)]
    [InlineData(UserRole.Management, "Course", true)]
    [InlineData(UserRole.Management, "Campus", true)]
    [InlineData(UserRole.Admin, "Official", true)]
    [InlineData(UserRole.Admin, "Course", true)]
    [InlineData(UserRole.Admin, "Campus", true)]
    public async Task CreateGroupAsync_UsesGlobalRolePermissionMatrix(UserRole role, string type, bool canCreate)
    {
        var user = new User
        {
            DisplayName = $"{role} User",
            Email = $"{role}@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = role
        };
        var service = new GroupsService(new FakeGroupRepository(), new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            $"{type} group",
            "Organizational group",
            "Campus",
            Type: type,
            CourseCode: type == "Course" ? "TIF25A" : null,
            OfficialCategory: type == "Official" ? "Verwaltung" : null));

        Assert.Equal(canCreate, result.IsSuccess);
        if (canCreate)
            Assert.Equal(type, result.Value!.Type);
        else
            Assert.Equal("This global role cannot create this group type.", result.Error);
    }

    [Fact]
    public async Task CreateGroupAsync_AppliesInitialSettingsFromCommand()
    {
        var user = new User
        {
            DisplayName = "Eva",
            Email = "eva@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var groups = new FakeGroupRepository();
        var service = new GroupsService(groups, new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            "Web study group",
            "Shared preparation",
            "Interested students",
            AllowStudentPosts: false,
            AllowComments: false,
            RequiresApproval: true,
            IsDiscoverable: false));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Settings.AllowStudentPosts);
        Assert.False(result.Value.Settings.AllowComments);
        Assert.True(result.Value.Settings.RequiresApproval);
        Assert.False(result.Value.Settings.IsDiscoverable);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_HidesPrivateUnassignedGroups()
    {
        var user = new User
        {
            DisplayName = "Jana",
            Email = "jana@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Kai",
            Email = "kai@dhbw-loerrach.de",
            StudyProgram = "Business Informatics",
            Semester = 2,
            Course = "WWI25A",
            Role = UserRole.Student
        };
        var privateGroup = SocialGroup(owner.Id, isDiscoverable: false);
        var service = new GroupsService(new FakeGroupRepository(privateGroup), new FakeUserRepository(user, owner));

        var result = await service.GetGroupsForUserAsync(user.Id);

        Assert.DoesNotContain(result, group => group.Id == privateGroup.Id);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_ShowsPublicUnassignedGroupsAsJoinable()
    {
        var user = new User
        {
            DisplayName = "Lea",
            Email = "lea@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Miro",
            Email = "miro@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Semester = 2,
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var publicGroup = SocialGroup(owner.Id);
        var service = new GroupsService(new FakeGroupRepository(publicGroup), new FakeUserRepository(user, owner));

        var result = await service.GetGroupsForUserAsync(user.Id);
        var group = Assert.Single(result, item => item.Id == publicGroup.Id);

        Assert.False(group.IsAssigned);
        Assert.False(group.CanPost);
        Assert.True(group.CanJoin);
    }

    [Fact]
    public async Task JoinGroupAsync_AssignsCurrentUserToPublicGroup()
    {
        var user = new User
        {
            DisplayName = "Nora",
            Email = "nora@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Oskar",
            Email = "oskar@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Semester = 2,
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var publicGroup = SocialGroup(owner.Id);
        var service = new GroupsService(new FakeGroupRepository(publicGroup), new FakeUserRepository(user, owner));

        var result = await service.JoinGroupAsync(publicGroup.Id, user.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsAssigned);
        Assert.True(result.Value.CanPost);
        Assert.False(result.Value.CanJoin);
    }

    [Fact]
    public async Task CreateGroupAsync_StoresJoinRuleAndOfficialCategory()
    {
        var user = new User
        {
            DisplayName = "Vera Management",
            Email = "vera@dhbw-loerrach.de",
            StudyProgram = "Campus Management",
            Semester = 1,
            Course = "ADM25A",
            Role = UserRole.Management
        };
        var service = new GroupsService(new FakeGroupRepository(), new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            "Exam office",
            "Official exam notices",
            "All students",
            Type: "Official",
            JoinRule: "RequestRequired",
            OfficialCategory: "Prüfungsamt"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Prüfungsamt", result.Value!.OfficialCategory);
        Assert.Equal("RequestRequired", result.Value.Settings.JoinRule);
    }

    [Fact]
    public async Task CreateGroupAsync_RejectsOfficialWithoutCategory()
    {
        var user = new User
        {
            DisplayName = "Vera Management",
            Email = "vera@dhbw-loerrach.de",
            StudyProgram = "Campus Management",
            Semester = 1,
            Course = "ADM25A",
            Role = UserRole.Management
        };
        var service = new GroupsService(new FakeGroupRepository(), new FakeUserRepository(user));

        var result = await service.CreateGroupAsync(new CreateGroupCommand(
            user.Id,
            "Exam office",
            "Official exam notices",
            "All students",
            Type: "Official"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Enter an official category for the official group.", result.Error);
    }

    [Fact]
    public async Task JoinGroupAsync_RequestRequired_CreatesPendingRequestWithoutMembership()
    {
        var user = new User
        {
            DisplayName = "Nora",
            Email = "nora@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Oskar",
            Email = "oskar@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Semester = 2,
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        group.Settings.JoinRule = GroupJoinRule.RequestRequired;
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(user, owner));

        var result = await service.JoinGroupAsync(group.Id, user.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsAssigned);
        Assert.True(result.Value.HasPendingJoinRequest);
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_AddsRequestingUserAsMember()
    {
        var user = new User
        {
            DisplayName = "Nora",
            Email = "nora@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Oskar",
            Email = "oskar@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Semester = 2,
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        group.Settings.JoinRule = GroupJoinRule.RequestRequired;
        group.PendingJoinRequests.Add(user.Id);
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(user, owner));

        var result = await service.ApproveJoinRequestAsync(group.Id, owner.Id, user.Id);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Members, member => member.Id == user.Id);
        Assert.Empty(result.Value.JoinRequests);
    }

    [Fact]
    public async Task InviteAndAccept_MakesInvitedUserAMember()
    {
        var user = new User
        {
            DisplayName = "Nora",
            Email = "nora@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Oskar",
            Email = "oskar@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Semester = 2,
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        group.Settings.JoinRule = GroupJoinRule.InviteOnly;
        var groups = new FakeGroupRepository(group);
        var service = new GroupsService(groups, new FakeUserRepository(user, owner));

        var invite = await service.InviteMembersAsync(group.Id, owner.Id, new InviteGroupMembersCommand([user.Id]));
        Assert.True(invite.IsSuccess);
        Assert.Contains(invite.Value!.Invitations, invitation => invitation.Id == user.Id);

        var accept = await service.RespondToInvitationAsync(group.Id, user.Id, accept: true);

        Assert.True(accept.IsSuccess);
        Assert.True(accept.Value!.IsAssigned);
        Assert.False(accept.Value.HasPendingInvitation);
    }

    [Fact]
    public async Task GetSettingsDetailsAsync_RejectsUnownedSocialGroup()
    {
        var user = new User
        {
            DisplayName = "Finn",
            Email = "finn@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
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
    public async Task AddMembersAsync_AddsExistingAccountsAsMembers()
    {
        var owner = new User
        {
            DisplayName = "Gina",
            Email = "gina@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Hannes",
            Email = "hannes@dhbw-loerrach.de",
            StudyProgram = "Business Informatics",
            Semester = 2,
            Course = "WWI25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        var groups = new FakeGroupRepository(group);
        var service = new GroupsService(groups, new FakeUserRepository(owner, member));

        var result = await service.AddMembersAsync(group.Id, owner.Id, new AddGroupMembersCommand([member.Id]));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Members, account => account.Id == owner.Id && account.IsOwner);
        Assert.Contains(result.Value.Members, account => account.Id == member.Id && account.GroupRole == "Member");
        Assert.Equal(2, result.Value.Group.AssignedUserCount);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMemberButNotOwner()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var member = StudentUser("Hannes", "hannes@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(member.Id);
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var ownerRemoval = await service.RemoveMemberAsync(group.Id, owner.Id, owner.Id);
        Assert.False(ownerRemoval.IsSuccess);
        Assert.Equal("The group owner cannot be removed.", ownerRemoval.Error);

        var result = await service.RemoveMemberAsync(group.Id, owner.Id, member.Id);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(result.Value!.Members, account => account.Id == member.Id);
        Assert.Equal(1, result.Value.Group.AssignedUserCount);
    }

    [Fact]
    public async Task GetSettingsDetailsAsync_AllowsAssignedModerator()
    {
        var owner = new User
        {
            DisplayName = "Gina",
            Email = "gina@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var manager = new User
        {
            DisplayName = "Iris",
            Email = "iris@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(manager.Id);
        group.MemberRoles[manager.Id] = GroupRole.Moderator;

        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, manager));

        var result = await service.GetSettingsDetailsAsync(group.Id, manager.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetSettingsDetailsAsync_RejectsPlainMember()
    {
        var owner = new User
        {
            DisplayName = "Gina",
            Email = "gina@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Hannes",
            Email = "hannes@dhbw-loerrach.de",
            StudyProgram = "Business Informatics",
            Semester = 2,
            Course = "WWI25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(member.Id);

        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.GetSettingsDetailsAsync(group.Id, member.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(GroupsService.PermissionError, result.Error);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_ExposesGroupRolesPerMember()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var moderator = StudentUser("Iris", "iris@dhbw-loerrach.de");
        var member = StudentUser("Hannes", "hannes@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(moderator.Id);
        group.AssignedUserIds.Add(member.Id);
        group.MemberRoles[moderator.Id] = GroupRole.Moderator;
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, moderator, member));

        var ownerGroup = Assert.Single(await service.GetGroupsForUserAsync(owner.Id), item => item.Id == group.Id);
        var moderatorGroup = Assert.Single(await service.GetGroupsForUserAsync(moderator.Id), item => item.Id == group.Id);
        var memberGroup = Assert.Single(await service.GetGroupsForUserAsync(member.Id), item => item.Id == group.Id);

        Assert.Equal("Owner", ownerGroup.GroupRole);
        Assert.Equal("Moderator", moderatorGroup.GroupRole);
        Assert.Equal("Member", memberGroup.GroupRole);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_AdminHasSystemAccessWithoutGroupRole()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var admin = new User
        {
            DisplayName = "Ada",
            Email = "ada@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Admin
        };
        var group = SocialGroup(owner.Id);
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, admin));

        var adminGroup = Assert.Single(await service.GetGroupsForUserAsync(admin.Id), item => item.Id == group.Id);

        Assert.Equal("None", adminGroup.GroupRole);
        Assert.True(adminGroup.IsSystemAdminAccess);
        Assert.True(adminGroup.CanManage);
        Assert.NotEqual(admin.Id, adminGroup.OwnerUserId);
    }

    [Fact]
    public async Task SetMemberRoleAsync_OwnerCanAppointModerator()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var member = StudentUser("Hannes", "hannes@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(member.Id);
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.SetMemberRoleAsync(group.Id, owner.Id, member.Id, new SetGroupMemberRoleCommand("Moderator"));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Members, account => account.Id == member.Id && account.GroupRole == "Moderator");
    }

    [Fact]
    public async Task SetMemberRoleAsync_ModeratorCannotAppointAnotherModerator()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var moderator = StudentUser("Iris", "iris@dhbw-loerrach.de");
        var member = StudentUser("Hannes", "hannes@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id);
        group.AssignedUserIds.Add(moderator.Id);
        group.AssignedUserIds.Add(member.Id);
        group.MemberRoles[moderator.Id] = GroupRole.Moderator;
        var service = new GroupsService(new FakeGroupRepository(group), new FakeUserRepository(owner, moderator, member));

        var result = await service.SetMemberRoleAsync(group.Id, moderator.Id, member.Id, new SetGroupMemberRoleCommand("Moderator"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Only the group owner can appoint moderators.", result.Error);
    }

    [Fact]
    public async Task DeleteGroupAsync_OwnerDeletesCampusGroupAndItsPosts()
    {
        var owner = StudentUser("Gina", "gina@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id);
        var groups = new FakeGroupRepository(group);
        var feed = new FakeFeedRepository(group.Id);
        var service = new GroupsService(groups, new FakeUserRepository(owner), feed);

        var result = await service.DeleteGroupAsync(group.Id, owner.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(await groups.FindByIdAsync(group.Id));
        Assert.Contains(group.Id, feed.DeletedGroupIds);
    }

    [Fact]
    public async Task DeleteGroupAsync_CourseGroupRequiresAdmin()
    {
        var lecturer = new User
        {
            DisplayName = "Lecturer",
            Email = "lecturer@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Lecturer
        };
        var admin = new User
        {
            DisplayName = "Admin",
            Email = "admin@dhbw-loerrach.de",
            StudyProgram = "Administration",
            Course = "ADMIN",
            Role = UserRole.Admin
        };
        var group = CourseGroup("TIF25A");
        group.AssignedUserIds.Add(lecturer.Id);
        var groups = new FakeGroupRepository(group);
        var feed = new FakeFeedRepository(group.Id);
        var service = new GroupsService(groups, new FakeUserRepository(lecturer, admin), feed);

        var denied = await service.DeleteGroupAsync(group.Id, lecturer.Id);
        var deleted = await service.DeleteGroupAsync(group.Id, admin.Id);

        Assert.False(denied.IsSuccess);
        Assert.Equal(GroupsService.PermissionError, denied.Error);
        Assert.True(deleted.IsSuccess);
    }

    private static User StudentUser(string displayName, string email) => new()
    {
        DisplayName = displayName,
        Email = email,
        StudyProgram = "Computer Science",
        Semester = 2,
        Course = "TIF25A",
        Role = UserRole.Student
    };

    private static CampusGroup CourseGroup(string courseCode) => new()
    {
        Name = $"Course {courseCode}",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = "Computer Science",
        IconLabel = "TI",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = false }
    };

    private static CampusGroup SocialGroup(Guid ownerId, bool isDiscoverable = true) => new()
    {
        Name = "Web study group",
        Description = "Shared preparation",
        Type = GroupType.Campus,
        Audience = "Interested students",
        OwnerUserId = ownerId,
        OwnerLabel = "Community",
        IconLabel = "LW",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = isDiscoverable },
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

        public Task DeleteAsync(Guid id)
        {
            _groups.RemoveAll(group => group.Id == id);
            return Task.CompletedTask;
        }

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings)
        {
            var group = _groups.First(group => group.Id == id);
            group.Settings = settings;
            return Task.CompletedTask;
        }

        public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds)
        {
            var group = _groups.First(group => group.Id == id);
            var assigned = group.AssignedUserIds.ToHashSet();
            foreach (var userId in userIds)
            {
                assigned.Add(userId);
                group.PendingJoinRequests.Remove(userId);
                group.Invitations.Remove(userId);
            }
            group.AssignedUserIds = assigned;
            return Task.CompletedTask;
        }

        public Task RemoveMemberAsync(Guid id, Guid userId)
        {
            var group = _groups.First(group => group.Id == id);
            var assigned = group.AssignedUserIds.ToHashSet();
            assigned.Remove(userId);
            group.AssignedUserIds = assigned;
            group.MemberRoles.Remove(userId);
            return Task.CompletedTask;
        }

        public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role)
        {
            var group = _groups.First(group => group.Id == id);
            if (role == GroupRole.Moderator)
                group.MemberRoles[userId] = GroupRole.Moderator;
            else
                group.MemberRoles.Remove(userId);
            return Task.CompletedTask;
        }

        public Task AddJoinRequestAsync(Guid id, Guid userId)
        {
            var group = _groups.First(group => group.Id == id);
            if (!group.AssignedUserIds.Contains(userId))
                group.PendingJoinRequests.Add(userId);
            return Task.CompletedTask;
        }

        public Task RemoveJoinRequestAsync(Guid id, Guid userId)
        {
            _groups.First(group => group.Id == id).PendingJoinRequests.Remove(userId);
            return Task.CompletedTask;
        }

        public Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds)
        {
            var group = _groups.First(group => group.Id == id);
            foreach (var userId in userIds)
            {
                if (!group.AssignedUserIds.Contains(userId))
                    group.Invitations.Add(userId);
            }
            return Task.CompletedTask;
        }

        public Task RemoveInvitationAsync(Guid id, Guid userId)
        {
            _groups.First(group => group.Id == id).Invitations.Remove(userId);
            return Task.CompletedTask;
        }

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
        {
            var group = _groups.FirstOrDefault(group => string.Equals(group.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase));
            if (group is not null)
                group.AssignedUserIds = assignedUserIds.ToHashSet();

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFeedRepository(params Guid[] groupIds) : IFeedRepository
    {
        public List<Guid> DeletedGroupIds { get; } = [];

        public Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize) =>
            Task.FromResult<IReadOnlyList<FeedPost>>([]);

        public Task<FeedPost?> FindByIdAsync(Guid id) => Task.FromResult<FeedPost?>(null);
        public Task AddAsync(FeedPost post) => Task.CompletedTask;
        public Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment) => Task.FromResult<FeedPost?>(null);
        public Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId) => Task.FromResult<FeedPost?>(null);
        public Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId) => Task.FromResult<FeedPost?>(null);
        public Task DeleteAsync(Guid id) => Task.CompletedTask;

        public Task DeleteByGroupAsync(Guid groupId)
        {
            Assert.Contains(groupId, groupIds);
            DeletedGroupIds.Add(groupId);
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
