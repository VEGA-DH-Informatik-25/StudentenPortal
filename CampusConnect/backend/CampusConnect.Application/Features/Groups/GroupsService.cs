using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Groups;

public record GroupSettingsDto(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record CampusGroupDto(Guid Id, string Name, string Description, string Type, string Audience, string? CourseCode, Guid? OwnerUserId, string OwnerLabel, string IconLabel, string AccentColor, int AssignedUserCount, bool CanManage, bool IsAssigned, bool CanPost, bool CanJoin, string MemberPermission, string GroupRole, bool IsSystemAdminAccess, bool CanAppointModerator, GroupSettingsDto Settings);
public record CreateGroupCommand(Guid CreatorId, string Name, string Description, string Audience, bool AllowStudentPosts = true, bool AllowComments = true, bool RequiresApproval = false, bool IsDiscoverable = true, string Type = "Social", string? CourseCode = null);
public record UpdateGroupSettingsCommand(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record UpdateGroupAssignmentsCommand(IReadOnlyList<Guid> UserIds);
public record UpdateGroupMemberPermissionCommand(Guid UserId, string Permission);
public record UpdateGroupMemberPermissionsCommand(IReadOnlyList<UpdateGroupMemberPermissionCommand> Permissions);
public record GroupAccountDto(Guid Id, string DisplayName, string Email, string Role, string Course, bool IsAssigned, string Permission, string GroupRole);
public record GroupSettingsDetailsDto(CampusGroupDto Group, IReadOnlyList<GroupAccountDto> Accounts);

public class GroupsService(IGroupRepository groupRepo, IUserRepository userRepo)
{
    public const string PermissionError = "You are not allowed to edit these group settings.";

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
            OwnerUserId = user.Id,
            OwnerLabel = user.DisplayName,
            IconLabel = Initials(command.Name),
            AccentColor = "#2563eb",
            Settings = new GroupSettings
            {
                AllowStudentPosts = command.AllowStudentPosts,
                AllowComments = command.AllowComments,
                RequiresApproval = command.RequiresApproval,
                IsDiscoverable = command.IsDiscoverable
            },
            AssignedUserIds = [user.Id]
        };

        await groupRepo.AddAsync(group);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(group, user));
    }

    public async Task<Result<GroupSettingsDetailsDto>> GetSettingsDetailsAsync(Guid groupId, Guid userId)
    {
        await SyncCourseGroupAssignmentsAsync();
        var context = await GetEditableGroupContextAsync(groupId, userId);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        return Result<GroupSettingsDetailsDto>.Success(await ToSettingsDetailsAsync(context.Value!.Group, context.Value.User));
    }

    public async Task<Result<CampusGroupDto>> UpdateSettingsAsync(Guid groupId, Guid userId, UpdateGroupSettingsCommand command)
    {
        var context = await GetEditableGroupContextAsync(groupId, userId);
        if (!context.IsSuccess)
            return Result<CampusGroupDto>.Failure(context.Error!);

        var settings = new GroupSettings
        {
            AllowStudentPosts = command.AllowStudentPosts,
            AllowComments = command.AllowComments,
            RequiresApproval = command.RequiresApproval,
            IsDiscoverable = command.IsDiscoverable
        };

        await groupRepo.UpdateSettingsAsync(groupId, settings);
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(updatedGroup!, context.Value!.User));
    }

    public async Task<Result<GroupSettingsDetailsDto>> UpdateAssignmentsAsync(Guid groupId, Guid userId, UpdateGroupAssignmentsCommand command)
    {
        var context = await GetEditableGroupContextAsync(groupId, userId);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        if (context.Value!.Group.Type == GroupType.Course)
            return Result<GroupSettingsDetailsDto>.Failure("Course groups are managed through user course assignments.");

        var users = await userRepo.ListAsync();
        var existingUserIds = users.Select(user => user.Id).ToHashSet();
        var assignedUserIds = command.UserIds
            .Where(existingUserIds.Contains)
            .Distinct()
            .ToHashSet();

        if (context.Value.Group.OwnerUserId is Guid ownerId)
            assignedUserIds.Add(ownerId);

        await groupRepo.UpdateAssignmentsAsync(groupId, assignedUserIds);
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<GroupSettingsDetailsDto>.Success(await ToSettingsDetailsAsync(updatedGroup!, context.Value.User));
    }

    public async Task<Result<GroupSettingsDetailsDto>> UpdateMemberPermissionsAsync(Guid groupId, Guid userId, UpdateGroupMemberPermissionsCommand command)
    {
        var context = await GetEditableGroupContextAsync(groupId, userId);
        if (!context.IsSuccess)
            return Result<GroupSettingsDetailsDto>.Failure(context.Error!);

        var canAppointModerator = GroupDtoMapper.CanAppointModerator(context.Value!.User, context.Value.Group);
        var permissionMap = new Dictionary<Guid, GroupMemberPermission>();
        foreach (var item in command.Permissions)
        {
            if (!context.Value!.Group.AssignedUserIds.Contains(item.UserId))
                continue;

            if (!TryParsePermission(item.Permission, out var permission))
                return Result<GroupSettingsDetailsDto>.Failure("Permission is invalid.");

            if (permission == GroupMemberPermission.Manage
                && !canAppointModerator
                && GroupDtoMapper.MemberPermissionFor(item.UserId, context.Value.Group) != GroupMemberPermission.Manage)
                return Result<GroupSettingsDetailsDto>.Failure("Only the group owner can appoint moderators.");

            permissionMap[item.UserId] = permission;
        }

        foreach (var assignedUserId in context.Value!.Group.AssignedUserIds)
        {
            if (!permissionMap.ContainsKey(assignedUserId))
                permissionMap[assignedUserId] = GroupDtoMapper.MemberPermissionFor(assignedUserId, context.Value.Group);
        }

        if (context.Value.Group.OwnerUserId is Guid ownerId && context.Value.Group.AssignedUserIds.Contains(ownerId))
            permissionMap[ownerId] = GroupMemberPermission.Manage;

        await groupRepo.UpdateMemberPermissionsAsync(groupId, permissionMap);
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<GroupSettingsDetailsDto>.Success(await ToSettingsDetailsAsync(updatedGroup!, context.Value.User));
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

        if (!GroupDtoMapper.CanJoin(user, group))
            return Result<CampusGroupDto>.Failure("You cannot join this group directly.");

        var assignedUserIds = group.AssignedUserIds.ToHashSet();
        assignedUserIds.Add(user.Id);

        await groupRepo.UpdateAssignmentsAsync(groupId, assignedUserIds);
        var updatedGroup = await groupRepo.FindByIdAsync(groupId);
        return Result<CampusGroupDto>.Success(GroupDtoMapper.ToDto(updatedGroup!, user));
    }

    private async Task<Result<GroupEditContext>> GetEditableGroupContextAsync(Guid groupId, Guid userId)
    {
        var group = await groupRepo.FindByIdAsync(groupId);
        if (group is null)
            return Result<GroupEditContext>.Failure("Group was not found.");

        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<GroupEditContext>.Failure("User profile was not found.");

        if (!GroupDtoMapper.CanManage(user, group))
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
        var accounts = users
            .OrderBy(account => account.DisplayName)
            .Select(account => new GroupAccountDto(
                account.Id,
                account.DisplayName,
                account.Email,
                account.Role.ToString(),
                account.Course,
                group.AssignedUserIds.Contains(account.Id),
                GroupDtoMapper.MemberPermissionFor(account.Id, group).ToString(),
                GroupDtoMapper.GroupRoleFor(account.Id, group).ToString()))
            .ToList();

        return new GroupSettingsDetailsDto(GroupDtoMapper.ToDto(group, user), accounts);
    }

    private static bool TryParsePermission(string value, out GroupMemberPermission permission) =>
        Enum.TryParse(value, ignoreCase: true, out permission) && Enum.IsDefined(permission);

    private static bool TryParseGroupType(string value, out GroupType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);

    private static bool CanCreateGroupType(UserRole role, GroupType type) =>
        type switch
        {
            GroupType.Social => true,
            GroupType.Course => role is UserRole.Lecturer or UserRole.Management or UserRole.Admin,
            GroupType.Official => role is UserRole.Management or UserRole.Admin,
            _ => false
        };

    private static string? NormalizeCourseCode(string? courseCode) =>
        string.IsNullOrWhiteSpace(courseCode) ? null : courseCode.Trim().ToUpperInvariant();

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
        group.OwnerUserId,
        group.OwnerLabel,
        group.IconLabel,
        group.AccentColor,
        group.AssignedUserIds.Count,
        currentUser is not null && CanManage(currentUser, group),
        currentUser is not null && IsAssigned(currentUser, group),
        currentUser is not null && CanPost(currentUser, group),
        currentUser is not null && CanJoin(currentUser, group),
        currentUser is null ? GroupMemberPermission.ReadOnly.ToString() : CurrentUserPermission(currentUser, group).ToString(),
        currentUser is null ? Domain.Enums.GroupRole.None.ToString() : GroupRoleFor(currentUser.Id, group).ToString(),
        currentUser is not null && IsSystemAdminAccess(currentUser, group),
        currentUser is not null && CanAppointModerator(currentUser, group),
        new GroupSettingsDto(
            group.Settings.AllowStudentPosts,
            group.Settings.AllowComments,
            group.Settings.RequiresApproval,
            group.Settings.IsDiscoverable));

    public static bool CanView(User? user, CampusGroup group) =>
        user is not null &&
        (user.Role == UserRole.Admin || IsAssigned(user, group) || group.OwnerUserId == user.Id || group.Settings.IsDiscoverable);

    public static bool CanReadPosts(User? user, CampusGroup group) =>
        user is not null &&
        (user.Role == UserRole.Admin || IsAssigned(user, group) || group.OwnerUserId == user.Id);

    public static bool CanManage(User user, CampusGroup group) =>
        user.Role == UserRole.Admin ||
        (IsAssigned(user, group) && CurrentUserPermission(user, group) == GroupMemberPermission.Manage) ||
        (user.Role == UserRole.Lecturer && group.Type == GroupType.Course && IsAssigned(user, group)) ||
        group.OwnerUserId == user.Id;

    public static bool CanPost(User user, CampusGroup group) =>
        user.Role == UserRole.Admin ||
        (CanWrite(user, group) && (user.Role != UserRole.Student || group.Settings.AllowStudentPosts));

    public static bool CanWrite(User user, CampusGroup group) =>
        user.Role == UserRole.Admin ||
        group.OwnerUserId == user.Id ||
        (IsAssigned(user, group) &&
         (CurrentUserPermission(user, group) == GroupMemberPermission.ReadWrite ||
          CurrentUserPermission(user, group) == GroupMemberPermission.Manage));

    public static bool CanJoin(User user, CampusGroup group) =>
        user.Role != UserRole.Admin &&
        group.Type == GroupType.Social &&
        group.Settings.IsDiscoverable &&
        !group.Settings.RequiresApproval &&
        !IsAssigned(user, group);

    public static bool IsAssigned(User user, CampusGroup group) => group.AssignedUserIds.Contains(user.Id);

    public static GroupMemberPermission MemberPermissionFor(Guid userId, CampusGroup group) =>
        group.OwnerUserId == userId
            ? GroupMemberPermission.Manage
            :
        group.MemberPermissions.TryGetValue(userId, out var permission) ? permission : GroupMemberPermission.ReadWrite;

    public static GroupRole GroupRoleFor(Guid userId, CampusGroup group)
    {
        if (group.OwnerUserId == userId)
            return GroupRole.Owner;

        if (!group.AssignedUserIds.Contains(userId))
            return GroupRole.None;

        return MemberPermissionFor(userId, group) == GroupMemberPermission.Manage
            ? GroupRole.Moderator
            : GroupRole.Member;
    }

    public static bool IsSystemAdminAccess(User user, CampusGroup group) =>
        user.Role == UserRole.Admin && GroupRoleFor(user.Id, group) == GroupRole.None;

    public static bool CanAppointModerator(User user, CampusGroup group) =>
        user.Role == UserRole.Admin || group.OwnerUserId == user.Id;

    public static bool CanDeleteGroup(User user, CampusGroup group) =>
        user.Role == UserRole.Admin || group.OwnerUserId == user.Id;

    private static GroupMemberPermission CurrentUserPermission(User user, CampusGroup group) =>
        IsAssigned(user, group) || user.Role == UserRole.Admin || group.OwnerUserId == user.Id ? MemberPermissionFor(user.Id, group) : GroupMemberPermission.ReadOnly;
}
