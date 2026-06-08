using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public sealed class EntityGroupRepository(CampusConnectDbContext dbContext) : IGroupRepository
{
    public async Task<IReadOnlyList<CampusGroup>> GetAllAsync() =>
        await dbContext.CampusGroups
            .AsNoTracking()
            .OrderBy(group => group.Type == GroupType.Official ? 0 : group.Type == GroupType.Course ? 1 : group.Type == GroupType.Campus ? 2 : 3)
            .ThenBy(group => group.CourseCode ?? group.Name)
            .Select(group => Clone(group))
            .ToListAsync();

    public async Task<CampusGroup?> FindByIdAsync(Guid id)
    {
        var group = await dbContext.CampusGroups.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return group is null ? null : Clone(group);
    }

    public async Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        var existing = await dbContext.CampusGroups.FirstOrDefaultAsync(group =>
            group.Type == GroupType.Course && group.CourseCode == normalizedCourse);

        if (existing is not null)
        {
            existing.Name = $"Course {normalizedCourse}";
            existing.Description = "Course-internal posts, study organization, and student-life notices.";
            existing.Audience = normalizedCourse;
            existing.OwnerLabel = string.IsNullOrWhiteSpace(studyProgram) ? "Course group" : studyProgram.Trim();
            existing.IconLabel = Initials(normalizedCourse);
            await dbContext.SaveChangesAsync();
            return Clone(existing);
        }

        var group = CreateCourseGroup(normalizedCourse, studyProgram);
        dbContext.CampusGroups.Add(group);
        await dbContext.SaveChangesAsync();
        return Clone(group);
    }

    public async Task AddAsync(CampusGroup group)
    {
        var existing = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == group.Id);
        if (existing is null)
        {
            dbContext.CampusGroups.Add(Clone(group));
        }
        else
        {
            Copy(group, existing);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateSettingsAsync(Guid id, GroupSettings settings)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        group.Settings = Clone(settings);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        var assigned = group.AssignedUserIds.ToHashSet();
        foreach (var userId in userIds)
            assigned.Add(userId);

        group.AssignedUserIds = assigned;

        var pending = group.PendingJoinRequests.ToHashSet();
        var invitations = group.Invitations.ToHashSet();
        foreach (var userId in userIds)
        {
            pending.Remove(userId);
            invitations.Remove(userId);
        }

        group.PendingJoinRequests = pending;
        group.Invitations = invitations;
        SyncMemberRoles(group);
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid id, Guid userId)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        var assigned = group.AssignedUserIds.ToHashSet();
        assigned.Remove(userId);
        group.AssignedUserIds = assigned;

        var roles = group.MemberRoles.ToDictionary(item => item.Key, item => item.Value);
        roles.Remove(userId);
        group.MemberRoles = roles;

        SyncMemberRoles(group);
        await dbContext.SaveChangesAsync();
    }

    public async Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null || !group.AssignedUserIds.Contains(userId))
            return;

        var roles = group.MemberRoles.ToDictionary(item => item.Key, item => item.Value);
        roles[userId] = role;
        group.MemberRoles = roles;

        SyncMemberRoles(group);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddJoinRequestAsync(Guid id, Guid userId)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null || group.AssignedUserIds.Contains(userId))
            return;

        var pending = group.PendingJoinRequests.ToHashSet();
        pending.Add(userId);
        group.PendingJoinRequests = pending;
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveJoinRequestAsync(Guid id, Guid userId)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        var pending = group.PendingJoinRequests.ToHashSet();
        if (!pending.Remove(userId))
            return;

        group.PendingJoinRequests = pending;
        await dbContext.SaveChangesAsync();
    }

    public async Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        var invitations = group.Invitations.ToHashSet();
        foreach (var userId in userIds)
        {
            if (!group.AssignedUserIds.Contains(userId))
                invitations.Add(userId);
        }

        group.Invitations = invitations;
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveInvitationAsync(Guid id, Guid userId)
    {
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return;

        var invitations = group.Invitations.ToHashSet();
        if (!invitations.Remove(userId))
            return;

        group.Invitations = invitations;
        await dbContext.SaveChangesAsync();
    }

    public async Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        var group = await dbContext.CampusGroups.FirstOrDefaultAsync(item =>
            item.Type == GroupType.Course && item.CourseCode == normalizedCourse);

        if (group is null)
            return;

        group.AssignedUserIds = assignedUserIds.ToHashSet();
        SyncMemberRoles(group);
        await dbContext.SaveChangesAsync();
    }

    private static CampusGroup CreateCourseGroup(string courseCode, string? studyProgram) => new()
    {
        Name = $"Course {courseCode}",
        Description = "Course-internal posts, study organization, and student-life notices.",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = string.IsNullOrWhiteSpace(studyProgram) ? "Course group" : studyProgram.Trim(),
        IconLabel = Initials(courseCode),
        AccentColor = "#e2001a",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = false }
    };

    private static void Copy(CampusGroup source, CampusGroup target)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Type = source.Type;
        target.Audience = source.Audience;
        target.CourseCode = source.CourseCode;
        target.OfficialCategory = source.OfficialCategory;
        target.OwnerUserId = source.OwnerUserId;
        target.OwnerLabel = source.OwnerLabel;
        target.IconLabel = source.IconLabel;
        target.AccentColor = source.AccentColor;
        target.Settings = Clone(source.Settings);
        target.AssignedUserIds = source.AssignedUserIds.ToHashSet();
        target.MemberRoles = source.MemberRoles.ToDictionary(item => item.Key, item => item.Value);
        target.PendingJoinRequests = source.PendingJoinRequests.ToHashSet();
        target.Invitations = source.Invitations.ToHashSet();
        SyncMemberRoles(target);
    }

    private static CampusGroup Clone(CampusGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        Type = group.Type,
        Audience = group.Audience,
        CourseCode = group.CourseCode,
        OfficialCategory = group.OfficialCategory,
        OwnerUserId = group.OwnerUserId,
        OwnerLabel = group.OwnerLabel,
        IconLabel = group.IconLabel,
        AccentColor = group.AccentColor,
        Settings = Clone(group.Settings),
        AssignedUserIds = group.AssignedUserIds.ToHashSet(),
        MemberRoles = group.MemberRoles.ToDictionary(item => item.Key, item => item.Value),
        PendingJoinRequests = group.PendingJoinRequests.ToHashSet(),
        Invitations = group.Invitations.ToHashSet()
    };

    private static void SyncMemberRoles(CampusGroup group)
    {
        var ownerId = group.OwnerUserId;
        group.MemberRoles = group.AssignedUserIds
            .Where(userId => userId != ownerId)
            .ToDictionary(
                userId => userId,
                userId => group.MemberRoles.TryGetValue(userId, out var role) && role is GroupRole.Moderator or GroupRole.Member
                    ? role
                    : GroupRole.Member);
    }

    private static GroupSettings Clone(GroupSettings settings) => new()
    {
        AllowStudentPosts = settings.AllowStudentPosts,
        AllowComments = settings.AllowComments,
        RequiresApproval = settings.RequiresApproval,
        IsDiscoverable = settings.IsDiscoverable,
        JoinRule = settings.JoinRule
    };

    private static string NormalizeCourse(string courseCode) => courseCode.Trim().ToUpperInvariant();

    private static string Initials(string value)
    {
        var normalized = NormalizeCourse(value);
        return normalized.Length <= 2 ? normalized : normalized[..2];
    }
}