using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryGroupRepository : IGroupRepository
{
    private readonly Dictionary<Guid, CampusGroup> _store = [];
    private readonly object _syncRoot = new();

    public Task<IReadOnlyList<CampusGroup>> GetAllAsync()
    {
        lock (_syncRoot)
        {
            var groups = _store.Values
                .OrderBy(group => SortKey(group.Type))
                .ThenBy(group => group.CourseCode ?? group.Name)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<CampusGroup>>(groups);
        }
    }

    public Task<CampusGroup?> FindByIdAsync(Guid id)
    {
        lock (_syncRoot)
        {
            _store.TryGetValue(id, out var group);
            return Task.FromResult(group is null ? null : Clone(group));
        }
    }

    public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        lock (_syncRoot)
        {
            var existing = _store.Values.FirstOrDefault(group =>
                group.Type == GroupType.Course &&
                string.Equals(group.CourseCode, normalizedCourse, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                return Task.FromResult(Clone(existing));

            var group = CreateCourseGroup(normalizedCourse, studyProgram);
            _store[group.Id] = group;
            return Task.FromResult(Clone(group));
        }
    }

    public Task AddAsync(CampusGroup group)
    {
        lock (_syncRoot)
        {
            _store[group.Id] = Clone(group);
        }

        return Task.CompletedTask;
    }

    public Task UpdateSettingsAsync(Guid id, GroupSettings settings)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                updated.Settings = Clone(settings);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                var assigned = updated.AssignedUserIds.ToHashSet();
                foreach (var userId in userIds)
                    assigned.Add(userId);

                updated.AssignedUserIds = assigned;
                var pending = updated.PendingJoinRequests.ToHashSet();
                var invitations = updated.Invitations.ToHashSet();
                foreach (var userId in userIds)
                {
                    pending.Remove(userId);
                    invitations.Remove(userId);
                }
                updated.PendingJoinRequests = pending;
                updated.Invitations = invitations;
                SyncMemberRoles(updated);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveMemberAsync(Guid id, Guid userId)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                var assigned = updated.AssignedUserIds.ToHashSet();
                assigned.Remove(userId);
                updated.AssignedUserIds = assigned;
                updated.MemberRoles.Remove(userId);
                SyncMemberRoles(updated);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group) && group.AssignedUserIds.Contains(userId))
            {
                var updated = Clone(group);
                updated.MemberRoles[userId] = role;
                SyncMemberRoles(updated);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        lock (_syncRoot)
        {
            var group = _store.Values.FirstOrDefault(group =>
                group.Type == GroupType.Course &&
                string.Equals(group.CourseCode, normalizedCourse, StringComparison.OrdinalIgnoreCase));

            if (group is not null)
            {
                var updated = Clone(group);
                updated.AssignedUserIds = assignedUserIds.ToHashSet();
                SyncMemberRoles(updated);
                _store[updated.Id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task AddJoinRequestAsync(Guid id, Guid userId)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group) && !group.AssignedUserIds.Contains(userId))
            {
                var updated = Clone(group);
                updated.PendingJoinRequests.Add(userId);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveJoinRequestAsync(Guid id, Guid userId)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                updated.PendingJoinRequests.Remove(userId);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                foreach (var userId in userIds)
                {
                    if (!updated.AssignedUserIds.Contains(userId))
                        updated.Invitations.Add(userId);
                }
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveInvitationAsync(Guid id, Guid userId)
    {
        lock (_syncRoot)
        {
            if (_store.TryGetValue(id, out var group))
            {
                var updated = Clone(group);
                updated.Invitations.Remove(userId);
                _store[id] = updated;
            }
        }

        return Task.CompletedTask;
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

    private static int SortKey(GroupType type) => type switch
    {
        GroupType.Official => 0,
        GroupType.Course => 1,
        GroupType.Campus => 2,
        _ => 3
    };
}
