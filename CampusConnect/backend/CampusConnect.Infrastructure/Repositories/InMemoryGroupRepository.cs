using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryGroupRepository : IGroupRepository
{
    private readonly ConcurrentDictionary<Guid, CampusGroup> _store = new();
    private readonly object _courseLock = new();

    public Task<IReadOnlyList<CampusGroup>> GetAllAsync()
    {
        var groups = _store.Values
            .OrderBy(group => SortKey(group.Type))
            .ThenBy(group => group.CourseCode ?? group.Name)
            .Select(Clone)
            .ToList();

        return Task.FromResult<IReadOnlyList<CampusGroup>>(groups);
    }

    public Task<CampusGroup?> FindByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var group);
        return Task.FromResult(group is null ? null : Clone(group));
    }

    public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        lock (_courseLock)
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
        _store[group.Id] = Clone(group);
        return Task.CompletedTask;
    }

    public Task UpdateSettingsAsync(Guid id, GroupSettings settings)
    {
        if (_store.TryGetValue(id, out var group))
        {
            group.Settings = Clone(settings);
            _store[id] = group;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds)
    {
        if (_store.TryGetValue(id, out var group))
        {
            group.AssignedUserIds = assignedUserIds.ToHashSet();
            _store[id] = group;
        }

        return Task.CompletedTask;
    }

    public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
    {
        var normalizedCourse = NormalizeCourse(courseCode);
        var group = _store.Values.FirstOrDefault(group =>
            group.Type == GroupType.Course &&
            string.Equals(group.CourseCode, normalizedCourse, StringComparison.OrdinalIgnoreCase));

        if (group is not null)
        {
            group.AssignedUserIds = assignedUserIds.ToHashSet();
            _store[group.Id] = group;
        }

        return Task.CompletedTask;
    }

    private static CampusGroup CreateCourseGroup(string courseCode, string? studyProgram) => new()
    {
        Name = $"Kurs {courseCode}",
        Description = "Kursinterne Beiträge, Lernorganisation und Hinweise für deinen Studienalltag.",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = string.IsNullOrWhiteSpace(studyProgram) ? "Kursgruppe" : studyProgram.Trim(),
        IconLabel = Initials(courseCode),
        AccentColor = "#e2001a",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
    };

    private static CampusGroup Clone(CampusGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        Type = group.Type,
        Audience = group.Audience,
        CourseCode = group.CourseCode,
        OwnerUserId = group.OwnerUserId,
        OwnerLabel = group.OwnerLabel,
        IconLabel = group.IconLabel,
        AccentColor = group.AccentColor,
        Settings = Clone(group.Settings),
        AssignedUserIds = group.AssignedUserIds.ToHashSet()
    };

    private static GroupSettings Clone(GroupSettings settings) => new()
    {
        AllowStudentPosts = settings.AllowStudentPosts,
        AllowComments = settings.AllowComments,
        RequiresApproval = settings.RequiresApproval,
        IsDiscoverable = settings.IsDiscoverable
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
        GroupType.Social => 2,
        _ => 3
    };
}
