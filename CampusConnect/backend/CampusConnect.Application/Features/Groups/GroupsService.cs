using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Groups;

public record GroupSettingsDto(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record CampusGroupDto(Guid Id, string Name, string Description, string Type, string Audience, string? CourseCode, Guid? OwnerUserId, string OwnerLabel, string IconLabel, string AccentColor, int AssignedUserCount, bool CanManage, GroupSettingsDto Settings);
public record CreateGroupCommand(Guid CreatorId, string Name, string Description, string Audience);
public record UpdateGroupSettingsCommand(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record UpdateGroupAssignmentsCommand(IReadOnlyList<Guid> UserIds);
public record GroupAccountDto(Guid Id, string DisplayName, string Email, string Role, string Course, bool IsAssigned);
public record GroupSettingsDetailsDto(CampusGroupDto Group, IReadOnlyList<GroupAccountDto> Accounts);

public class GroupsService(IGroupRepository groupRepo, IUserRepository userRepo)
{
    public const string PermissionError = "Keine Berechtigung zum Bearbeiten dieser Gruppeneinstellungen.";

    public async Task<IReadOnlyList<CampusGroupDto>> GetGroupsForUserAsync(Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId);
        if (user is not null && user.Role != UserRole.Admin && !string.IsNullOrWhiteSpace(user.Course))
            await groupRepo.EnsureCourseGroupAsync(user.Course, user.StudyProgram);

        await SyncCourseGroupAssignmentsAsync();
        var groups = await groupRepo.GetAllAsync();
        return groups.Select(group => GroupDtoMapper.ToDto(group, user)).ToList();
    }

    public async Task<Result<CampusGroupDto>> CreateGroupAsync(CreateGroupCommand command)
    {
        var user = await userRepo.FindByIdAsync(command.CreatorId);
        if (user is null)
            return Result<CampusGroupDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        var validationError = ValidateGroup(command.Name, command.Description, command.Audience);
        if (validationError is not null)
            return Result<CampusGroupDto>.Failure(validationError);

        var group = new CampusGroup
        {
            Name = command.Name.Trim(),
            Description = command.Description.Trim(),
            Type = GroupType.Social,
            Audience = command.Audience.Trim(),
            OwnerUserId = user.Id,
            OwnerLabel = user.DisplayName,
            IconLabel = Initials(command.Name),
            AccentColor = "#2563eb",
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true },
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
            return Result<GroupSettingsDetailsDto>.Failure("Kursgruppen werden über die Kurszuordnung der Benutzer verwaltet.");

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

    private async Task<Result<GroupEditContext>> GetEditableGroupContextAsync(Guid groupId, Guid userId)
    {
        var group = await groupRepo.FindByIdAsync(groupId);
        if (group is null)
            return Result<GroupEditContext>.Failure("Gruppe wurde nicht gefunden.");

        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<GroupEditContext>.Failure("Benutzerprofil wurde nicht gefunden.");

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
                group.AssignedUserIds.Contains(account.Id)))
            .ToList();

        return new GroupSettingsDetailsDto(GroupDtoMapper.ToDto(group, user), accounts);
    }

    private static string? ValidateGroup(string name, string description, string audience)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(audience))
            return "Bitte fülle alle Gruppenfelder aus.";

        if (name.Trim().Length > 80)
            return "Der Gruppenname darf höchstens 80 Zeichen lang sein.";

        if (description.Trim().Length > 240)
            return "Die Beschreibung darf höchstens 240 Zeichen lang sein.";

        if (audience.Trim().Length > 80)
            return "Die Zielgruppe darf höchstens 80 Zeichen lang sein.";

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
        new GroupSettingsDto(
            group.Settings.AllowStudentPosts,
            group.Settings.AllowComments,
            group.Settings.RequiresApproval,
            group.Settings.IsDiscoverable));

    public static bool CanManage(User user, CampusGroup group) =>
        user.Role == UserRole.Admin ||
        (user.Role == UserRole.Lecturer && group.Type == GroupType.Course) ||
        group.OwnerUserId == user.Id;
}
