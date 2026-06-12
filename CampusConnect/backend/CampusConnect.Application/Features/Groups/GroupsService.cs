using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Groups;

public record GroupSettingsDto(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable, string JoinRule);
public record CampusGroupDto(
    Guid Id,
    string Name,
    string Description,
    string Type,
    string Audience,
    string? CourseCode,
    string? OfficialCategory,
    Guid? OwnerUserId,
    string OwnerLabel,
    string IconLabel,
    string AccentColor,
    int AssignedUserCount,
    bool IsAssigned,
    bool CanManage,
    bool CanEditSettings,
    bool CanManageMembers,
    bool CanAppointModerator,
    bool CanPost,
    bool CanInteract,
    bool CanJoin,
    bool CanRequestJoin,
    bool HasPendingJoinRequest,
    bool HasPendingInvitation,
    int PendingJoinRequestCount,
    bool CanDelete,
    string GroupRole,
    bool IsSystemAdminAccess,
    bool IsCourseManaged,
    GroupSettingsDto Settings);
public record CreateGroupCommand(Guid CreatorId, string Name, string Description, string Audience, bool AllowStudentPosts = true, bool AllowComments = true, bool RequiresApproval = false, bool IsDiscoverable = true, string Type = "Campus", string? CourseCode = null, string JoinRule = "Open", string? OfficialCategory = null);
public record UpdateGroupSettingsCommand(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable, string JoinRule = "Open");
public record AddGroupMembersCommand(IReadOnlyList<Guid> UserIds);
public record InviteGroupMembersCommand(IReadOnlyList<Guid> UserIds);
public record AddGroupCourseCommand(string CourseCode);
public record SetGroupMemberRoleCommand(string Role);
public record GroupMemberDto(Guid Id, string DisplayName, string Email, string Role, string Course, string GroupRole, bool IsOwner);
public record GroupRequestDto(Guid Id, string DisplayName, string Email, string Role, string Course);
public record GroupCandidateDto(Guid Id, string DisplayName, string Email, string Role, string Course);
public record GroupSettingsDetailsDto(CampusGroupDto Group, IReadOnlyList<GroupMemberDto> Members, IReadOnlyList<GroupRequestDto> JoinRequests, IReadOnlyList<GroupRequestDto> Invitations);

public class GroupsService(IGroupRepository groupRepo, IUserRepository userRepo, IFeedRepository? feedRepo = null)
{
    public const string PermissionError = "You are not allowed to manage this group.";
    private const string CourseManagedError = "Course group membership is managed through course assignments.";
    private const int CandidateLimit = 25;

    public async Task<IReadOnlyList<CampusGroupDto>> GetGroupsForUserAsync(Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId);
        if (user is not null && user.Role != UserRole.Admin && !string.IsNullOrWhiteSpace(user.Course))
            await groupRepo.EnsureCourseGroupAsync(user.Course, user.StudyProgram);

        await SyncCourseGroupAssignmentsAsync();
        var groups = await groupRepo.GetAllAsync();
        return groups
            .Where(group => GroupDtoMapper.CanView(user, group))
            .Select(group => GroupDtoMapper.ToDto(group, user))
            .ToList();
    }

    public async Task<Result<CampusGroupDto>> CreateGroupAsync(CreateGroupCommand command)
    {
        var user = await userRepo.FindByIdAsync(command.CreatorId);
        if (user is null)
            return Result<CampusGroupDto>.Failure("User profile was not found.");

        var validationError = ValidateGroup(command.Name, command.Description, command.Audience);
        if (validationError is not null)
            return Result<CampusGroupDto>.Failure(validationError);

        if (!TryParseGroupType(command.Type, out var groupType))
            return Result<CampusGroupDto>.Failure("Group type is invalid.");

        if (!CanCreateGroupType(user.Role, groupType))
            return Result<CampusGroupDto>.Failure("This global role cannot create this group type.");

        if (!TryParseJoinRule(command.JoinRule, out var joinRule))
            return Result<CampusGroupDto>.Failure("Join rule is invalid.");

        var officialCategory = NormalizeOfficialCategory(command.OfficialCategory);
        if (groupType == GroupType.Official && officialCategory is null)
            return Result<CampusGroupDto>.Failure("Enter an official category for the official group.");

        if (officialCategory is { Length: > 80 })
            return Result<CampusGroupDto>.Failure("Official category must be at most 80 characters long.");

        var courseCode = NormalizeCourseCode(command.CourseCode);
        if (groupType == GroupType.Course)
        {
            if (courseCode is null)
                return Result<CampusGroupDto>.Failure("Enter a course code for the course group.");

            if (courseCode.Length > 40)
                return Result<CampusGroupDto>.Failure("Course code must be at most 40 characters long.");

            var existingGroups = await groupRepo.GetAllAsync();
            if (existingGroups.Any(group => group.Type == GroupType.Course && string.Equals(group.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase)))
                return Result<CampusGroupDto>.Failure("A course group already exists for this course.");
        }

        var group = new CampusGroup
        {
            Name = command.Name.Trim(),
            Description = command.Description.Trim(),
            Type = groupType,
            Audience = command.Audience.Trim(),
            CourseCode = groupType == GroupType.Course ? courseCode : null,
            OfficialCategory = groupType == GroupType.Official ? officialCategory : null,
            OwnerUserId = user.Id,
            OwnerLabel = user.DisplayName,
            IconLabel = Initials(command.Name),
            AccentColor = "#2563eb",
            Settings = new GroupSettings
            {
                AllowStudentPosts = command.AllowStudentPosts,
                AllowComments = command.AllowComments,
                RequiresApproval = command.RequiresApproval,
                IsDiscoverable = command.IsDiscoverable,
                JoinRule = joinRule
            },
            AssignedUserIds = [user.Id]
        };

        await groupRepo.AddAsync(group);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(group, user));
    }

    public async Task<Result<GroupSettingsDetailsDto>> GetSettingsDetailsAsync(Guid groupId, Guid userId)
    {
        await SyncCourseGroupAssignmentsAsync();
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManage);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        return Result<GroupSettingsDetailsDto>.Success(await ToSettingsDetailsAsync(context.Value!.Group, context.Value.User));
    }

    public async Task<Result<CampusGroupDto>> UpdateSettingsAsync(Guid groupId, Guid userId, UpdateGroupSettingsCommand command)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanEditSettings);
        if (!context.IsSuccess)
            return Result<CampusGroupDto>.Failure(context.Error!);

        if (!TryParseJoinRule(command.JoinRule, out var joinRule))
            return Result<CampusGroupDto>.Failure("Join rule is invalid.");

        var settings = new GroupSettings
        {
            AllowStudentPosts = command.AllowStudentPosts,
            AllowComments = command.AllowComments,
            RequiresApproval = command.RequiresApproval,
            IsDiscoverable = command.IsDiscoverable,
            JoinRule = joinRule
        };

        await groupRepo.UpdateSettingsAsync(groupId, settings);
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(updatedGroup!, context.Value!.User));
    }

    public async Task<Result<bool>> DeleteGroupAsync(Guid groupId, Guid userId)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanDeleteGroup);
        if (!context.IsSuccess)
            return Result<bool>.Failure(context.Error!);

        if (feedRepo is null)
            return Result<bool>.Failure("Group deletion is not available.");

        await feedRepo.DeleteByGroupAsync(groupId);
        await groupRepo.DeleteAsync(groupId);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<GroupCandidateDto>>> SearchCandidatesAsync(Guid groupId, Guid userId, string? query)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<IReadOnlyList<GroupCandidateDto>>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<IReadOnlyList<GroupCandidateDto>>.Failure(CourseManagedError);

        var assigned = context.Value.Group.AssignedUserIds;
        var term = query?.Trim() ?? string.Empty;
        var users = await userRepo.ListAsync();

        var candidates = users
            .Where(account => !assigned.Contains(account.Id))
            .Where(account => MatchesQuery(account, term))
            .OrderBy(account => account.DisplayName)
            .Take(CandidateLimit)
            .Select(account => new GroupCandidateDto(account.Id, account.DisplayName, account.Email, account.Role.ToString(), account.Course))
            .ToList();

        return Result<IReadOnlyList<GroupCandidateDto>>.Success(candidates);
    }

    public async Task<Result<GroupSettingsDetailsDto>> AddMembersAsync(Guid groupId, Guid userId, AddGroupMembersCommand command)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure(CourseManagedError);

        var users = await userRepo.ListAsync();
        var existingUserIds = users.Select(account => account.Id).ToHashSet();
        var toAdd = command.UserIds.Where(existingUserIds.Contains).Distinct().ToList();
        if (toAdd.Count == 0)
            return Result<GroupSettingsDetailsDto>.Failure("Select at least one valid account to add.");

        await groupRepo.AddMembersAsync(groupId, toAdd);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> AddCourseMembersAsync(Guid groupId, Guid userId, AddGroupCourseCommand command)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure(CourseManagedError);

        var courseCode = NormalizeCourseCode(command.CourseCode);
        if (courseCode is null)
            return Result<GroupSettingsDetailsDto>.Failure("Select a course to add.");

        var users = await userRepo.ListAsync();
        var courseUserIds = users
            .Where(account => string.Equals(account.Course, courseCode, StringComparison.OrdinalIgnoreCase))
            .Select(account => account.Id)
            .ToList();

        if (courseUserIds.Count == 0)
            return Result<GroupSettingsDetailsDto>.Failure("No accounts are assigned to this course.");

        await groupRepo.AddMembersAsync(groupId, courseUserIds);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> RemoveMemberAsync(Guid groupId, Guid userId, Guid targetUserId)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure(CourseManagedError);

        if (context.Value.Group.OwnerUserId == targetUserId)
            return Result<GroupSettingsDetailsDto>.Failure("The group owner cannot be removed.");

        if (!context.Value.Group.AssignedUserIds.Contains(targetUserId))
            return Result<GroupSettingsDetailsDto>.Failure("This account is not a member of the group.");

        await groupRepo.RemoveMemberAsync(groupId, targetUserId);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> SetMemberRoleAsync(Guid groupId, Guid userId, Guid targetUserId, SetGroupMemberRoleCommand command)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        var group = context.Value!.Group;
        if (group.OwnerUserId == targetUserId)
            return Result<GroupSettingsDetailsDto>.Failure("The group owner role cannot be changed.");

        if (!group.AssignedUserIds.Contains(targetUserId))
            return Result<GroupSettingsDetailsDto>.Failure("This account is not a member of the group.");

        if (!TryParseMemberRole(command.Role, out var role))
            return Result<GroupSettingsDetailsDto>.Failure("Group role is invalid.");

        if (role == GroupRole.Moderator && !GroupDtoMapper.CanAppointModerator(context.Value.User, group))
            return Result<GroupSettingsDetailsDto>.Failure("Only the group owner can appoint moderators.");

        await groupRepo.SetMemberRoleAsync(groupId, targetUserId, role);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<CampusGroupDto>> JoinGroupAsync(Guid groupId, Guid userId)
    {
        await SyncCourseGroupAssignmentsAsync();
        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<CampusGroupDto>.Failure("User profile was not found.");

        var group = await groupRepo.FindByIdAsync(groupId);
        if (group is null || !GroupDtoMapper.CanView(user, group))
            return Result<CampusGroupDto>.Failure("Group was not found.");

        if (user.Role == UserRole.Admin || group.Type == GroupType.Course || GroupDtoMapper.IsAssigned(user, group))
            return Result<CampusGroupDto>.Failure("You cannot join this group directly.");

        // An open invitation always lets the user join, regardless of the join rule.
        if (group.Invitations.Contains(user.Id))
        {
            await groupRepo.AddMembersAsync(groupId, [user.Id]);
            return await ReloadGroupAsync(groupId, user);
        }

        switch (group.Settings.JoinRule)
        {
            case GroupJoinRule.Open when group.Settings.IsDiscoverable:
                await groupRepo.AddMembersAsync(groupId, [user.Id]);
                return await ReloadGroupAsync(groupId, user);

            case GroupJoinRule.RequestRequired when group.Settings.IsDiscoverable:
                if (group.PendingJoinRequests.Contains(user.Id))
                    return Result<CampusGroupDto>.Failure("Your join request is already pending.");

                await groupRepo.AddJoinRequestAsync(groupId, user.Id);
                return await ReloadGroupAsync(groupId, user);

            default:
                return Result<CampusGroupDto>.Failure("You cannot join this group directly.");
        }
    }

    public async Task<Result<GroupSettingsDetailsDto>> ApproveJoinRequestAsync(Guid groupId, Guid userId, Guid targetUserId)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure(CourseManagedError);

        if (!context.Value.Group.PendingJoinRequests.Contains(targetUserId))
            return Result<GroupSettingsDetailsDto>.Failure("There is no pending join request for this account.");

        await groupRepo.AddMembersAsync(groupId, [targetUserId]);
        await groupRepo.RemoveJoinRequestAsync(groupId, targetUserId);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> RejectJoinRequestAsync(Guid groupId, Guid userId, Guid targetUserId)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (!context.Value!.Group.PendingJoinRequests.Contains(targetUserId))
            return Result<GroupSettingsDetailsDto>.Failure("There is no pending join request for this account.");

        await groupRepo.RemoveJoinRequestAsync(groupId, targetUserId);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> InviteMembersAsync(Guid groupId, Guid userId, InviteGroupMembersCommand command)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        var group = context.Value!.Group;
        if (group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure(CourseManagedError);

        var users = await userRepo.ListAsync();
        var existingUserIds = users.Select(account => account.Id).ToHashSet();
        var toInvite = command.UserIds
            .Where(existingUserIds.Contains)
            .Where(id => !group.AssignedUserIds.Contains(id))
            .Distinct()
            .ToList();

        if (toInvite.Count == 0)
            return Result<GroupSettingsDetailsDto>.Failure("Select at least one account that is not already a member.");

        await groupRepo.AddInvitationsAsync(groupId, toInvite);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<GroupSettingsDetailsDto>> CancelInvitationAsync(Guid groupId, Guid userId, Guid targetUserId)
    {
        var context = await GetGroupContextAsync(groupId, userId, GroupDtoMapper.CanManageMembers);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (!context.Value!.Group.Invitations.Contains(targetUserId))
            return Result<GroupSettingsDetailsDto>.Failure("There is no pending invitation for this account.");

        await groupRepo.RemoveInvitationAsync(groupId, targetUserId);
        return await ReloadDetailsAsync(groupId, context.Value.User);
    }

    public async Task<Result<CampusGroupDto>> RespondToInvitationAsync(Guid groupId, Guid userId, bool accept)
    {
        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<CampusGroupDto>.Failure("User profile was not found.");

        var group = await groupRepo.FindByIdAsync(groupId);
        if (group is null)
            return Result<CampusGroupDto>.Failure("Group was not found.");

        if (!group.Invitations.Contains(user.Id))
            return Result<CampusGroupDto>.Failure("You do not have a pending invitation for this group.");

        if (accept)
            await groupRepo.AddMembersAsync(groupId, [user.Id]);
        else
            await groupRepo.RemoveInvitationAsync(groupId, user.Id);

        return await ReloadGroupAsync(groupId, user);
    }

    private async Task<Result<CampusGroupDto>> ReloadGroupAsync(Guid groupId, User user)
    {
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(updatedGroup!, user));
    }

    private async Task<Result<GroupSettingsDetailsDto>> ReloadDetailsAsync(Guid groupId, User user)
    {
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<GroupSettingsDetailsDto>.Success(await ToSettingsDetailsAsync(updatedGroup!, user));
    }

    private async Task<Result<GroupEditContext>> GetGroupContextAsync(Guid groupId, Guid userId, Func<User, CampusGroup, bool> capability)
    {
        var group = await groupRepo.FindByIdAsync(groupId);
        if (group is null)
            return Result<GroupEditContext>.Failure("Group was not found.");

        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<GroupEditContext>.Failure("User profile was not found.");

        if (!capability(user, group))
            return Result<GroupEditContext>.Failure(PermissionError);

        return Result<GroupEditContext>.Success(new GroupEditContext(group, user));
    }

    private async Task SyncCourseGroupAssignmentsAsync()
    {
        var users = await userRepo.ListAsync();
        var groups = await groupRepo.GetAllAsync();
        foreach (var group in groups.Where(group => group.Type == GroupType.Course && !string.IsNullOrWhiteSpace(group.CourseCode)))
        {
            var assignedUserIds = users
                .Where(user => string.Equals(user.Course, group.CourseCode, StringComparison.OrdinalIgnoreCase))
                .Select(user => user.Id)
                .ToList();

            await groupRepo.SyncCourseAssignmentsAsync(group.CourseCode!, assignedUserIds);
        }
    }

    private async Task<GroupSettingsDetailsDto> ToSettingsDetailsAsync(CampusGroup group, User user)
    {
        var users = await userRepo.ListAsync();
        var byId = users.ToDictionary(account => account.Id);

        var members = group.AssignedUserIds
            .Where(byId.ContainsKey)
            .Select(memberId => byId[memberId])
            .OrderByDescending(account => GroupDtoMapper.GroupRoleFor(account.Id, group))
            .ThenBy(account => account.DisplayName)
            .Select(account => new GroupMemberDto(
                account.Id,
                account.DisplayName,
                account.Email,
                account.Role.ToString(),
                account.Course,
                GroupDtoMapper.GroupRoleFor(account.Id, group).ToString(),
                group.OwnerUserId == account.Id))
            .ToList();

        var joinRequests = group.PendingJoinRequests
            .Where(byId.ContainsKey)
            .Select(requestId => byId[requestId])
            .OrderBy(account => account.DisplayName)
            .Select(account => new GroupRequestDto(account.Id, account.DisplayName, account.Email, account.Role.ToString(), account.Course))
            .ToList();

        var invitations = group.Invitations
            .Where(byId.ContainsKey)
            .Select(inviteId => byId[inviteId])
            .OrderBy(account => account.DisplayName)
            .Select(account => new GroupRequestDto(account.Id, account.DisplayName, account.Email, account.Role.ToString(), account.Course))
            .ToList();

        return new GroupSettingsDetailsDto(GroupDtoMapper.ToDto(group, user), members, joinRequests, invitations);
    }

    private static bool MatchesQuery(User account, string term)
    {
        if (term.Length == 0)
            return true;

        return account.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || account.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
            || account.Course.Contains(term, StringComparison.OrdinalIgnoreCase)
            || account.Role.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseMemberRole(string value, out GroupRole role)
    {
        role = GroupRole.Member;
        if (!Enum.TryParse(value, ignoreCase: true, out GroupRole parsed) || !Enum.IsDefined(parsed))
            return false;

        if (parsed is not (GroupRole.Member or GroupRole.Moderator))
            return false;

        role = parsed;
        return true;
    }

    private static bool TryParseGroupType(string value, out GroupType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);

    private static bool TryParseJoinRule(string value, out GroupJoinRule joinRule) =>
        Enum.TryParse(value, ignoreCase: true, out joinRule) && Enum.IsDefined(joinRule);

    private static bool CanCreateGroupType(UserRole role, GroupType type) =>
        type switch
        {
            GroupType.Campus => true,
            GroupType.Course => role is UserRole.Lecturer or UserRole.Management or UserRole.Admin,
            GroupType.Official => role is UserRole.Management or UserRole.Admin,
            _ => false
        };

    private static string? NormalizeCourseCode(string? courseCode) =>
        string.IsNullOrWhiteSpace(courseCode) ? null : courseCode.Trim().ToUpperInvariant();

    private static string? NormalizeOfficialCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : category.Trim();

    private static string? ValidateGroup(string name, string description, string audience)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(audience))
            return "Fill in all group fields.";

        if (name.Trim().Length > 80)
            return "Group name must be at most 80 characters long.";

        if (description.Trim().Length > 240)
            return "Description must be at most 240 characters long.";

        if (audience.Trim().Length > 80)
            return "Audience must be at most 80 characters long.";

        return null;
    }

    private static string Initials(string value)
    {
        var words = value
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
            return "GR";

        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    private sealed record GroupEditContext(CampusGroup Group, User User);
}

public static class GroupDtoMapper
{
    public static CampusGroupDto ToDto(CampusGroup group, User? currentUser = null) => new(
        group.Id,
        group.Name,
        group.Description,
        group.Type.ToString(),
        group.Audience,
        group.CourseCode,
        group.OfficialCategory,
        group.OwnerUserId,
        group.OwnerLabel,
        group.IconLabel,
        group.AccentColor,
        group.AssignedUserIds.Count,
        currentUser is not null && IsAssigned(currentUser, group),
        currentUser is not null && CanManage(currentUser, group),
        currentUser is not null && CanEditSettings(currentUser, group),
        currentUser is not null && CanManageMembers(currentUser, group),
        currentUser is not null && CanAppointModerator(currentUser, group),
        currentUser is not null && CanPost(currentUser, group),
        currentUser is not null && CanInteract(currentUser, group),
        currentUser is not null && CanJoin(currentUser, group),
        currentUser is not null && CanRequestJoin(currentUser, group),
        currentUser is not null && group.PendingJoinRequests.Contains(currentUser.Id),
            currentUser is not null && group.Invitations.Contains(currentUser.Id),
            CanManageMembersFor(currentUser, group) ? group.PendingJoinRequests.Count : 0,
            currentUser is not null && CanDeleteGroup(currentUser, group),
            currentUser is null ? GroupRole.None.ToString() : GroupRoleFor(currentUser.Id, group).ToString(),
        currentUser is not null && IsSystemAdminAccess(currentUser, group),
        group.Type == GroupType.Course,
        new GroupSettingsDto(
            group.Settings.AllowStudentPosts,
            group.Settings.AllowComments,
            group.Settings.RequiresApproval,
            group.Settings.IsDiscoverable,
            group.Settings.JoinRule.ToString()));

    private static bool CanManageMembersFor(User? user, CampusGroup group) => user is not null && CanManageMembers(user, group);

    public static bool CanView(User? user, CampusGroup group) =>
        user is not null &&
        (user.Role == UserRole.Admin || IsAssigned(user, group) || group.OwnerUserId == user.Id || group.Settings.IsDiscoverable);

    public static bool CanReadPosts(User? user, CampusGroup group) =>
        user is not null &&
        (user.Role == UserRole.Admin || IsAssigned(user, group) || group.OwnerUserId == user.Id);

    public static bool CanManage(User user, CampusGroup group)
    {
        var role = GroupRoleFor(user.Id, group);
        return user.Role == UserRole.Admin
            || role is GroupRole.Owner or GroupRole.Moderator
            || IsCourseLecturer(user, group);
    }

    public static bool CanEditSettings(User user, CampusGroup group) =>
        user.Role == UserRole.Admin
        || GroupRoleFor(user.Id, group) == GroupRole.Owner
        || IsCourseLecturer(user, group);

    public static bool CanManageMembers(User user, CampusGroup group)
    {
        var role = GroupRoleFor(user.Id, group);
        return user.Role == UserRole.Admin
            || role is GroupRole.Owner or GroupRole.Moderator
            || IsCourseLecturer(user, group);
    }

    public static bool CanAppointModerator(User user, CampusGroup group) =>
        user.Role == UserRole.Admin || group.OwnerUserId == user.Id;

    public static bool CanPost(User user, CampusGroup group)
    {
        if (user.Role == UserRole.Admin)
            return true;

        return GroupRoleFor(user.Id, group) switch
        {
            GroupRole.Owner or GroupRole.Moderator => true,
            GroupRole.Member => group.Settings.AllowStudentPosts,
            _ => IsCourseLecturer(user, group)
        };
    }

    public static bool CanInteract(User user, CampusGroup group) =>
        user.Role == UserRole.Admin
        || group.OwnerUserId == user.Id
        || IsAssigned(user, group)
        || IsCourseLecturer(user, group);

    public static bool CanJoin(User user, CampusGroup group) =>
        user.Role != UserRole.Admin &&
        group.Type != GroupType.Course &&
        !IsAssigned(user, group) &&
        (group.Invitations.Contains(user.Id) ||
            (group.Settings.IsDiscoverable && group.Settings.JoinRule == GroupJoinRule.Open));

    public static bool CanRequestJoin(User user, CampusGroup group) =>
        user.Role != UserRole.Admin &&
        group.Type != GroupType.Course &&
        !IsAssigned(user, group) &&
        !group.Invitations.Contains(user.Id) &&
        !group.PendingJoinRequests.Contains(user.Id) &&
        group.Settings.IsDiscoverable &&
        group.Settings.JoinRule == GroupJoinRule.RequestRequired;

    public static bool IsAssigned(User user, CampusGroup group) => group.AssignedUserIds.Contains(user.Id);

    public static GroupRole GroupRoleFor(Guid userId, CampusGroup group)
    {
        if (group.OwnerUserId == userId)
            return GroupRole.Owner;

        if (!group.AssignedUserIds.Contains(userId))
            return GroupRole.None;

        return group.MemberRoles.TryGetValue(userId, out var role) && role == GroupRole.Moderator
            ? GroupRole.Moderator
            : GroupRole.Member;
    }

    public static bool IsSystemAdminAccess(User user, CampusGroup group) =>
        user.Role == UserRole.Admin && GroupRoleFor(user.Id, group) == GroupRole.None;

    public static bool CanDeleteGroup(User user, CampusGroup group) =>
        user.Role == UserRole.Admin || (group.Type != GroupType.Course && group.OwnerUserId == user.Id);

    private static bool IsCourseLecturer(User user, CampusGroup group) =>
        user.Role == UserRole.Lecturer && group.Type == GroupType.Course && IsAssigned(user, group);
}
