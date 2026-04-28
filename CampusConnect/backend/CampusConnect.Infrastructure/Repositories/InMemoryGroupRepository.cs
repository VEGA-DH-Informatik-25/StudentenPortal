using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryGroupRepository : IGroupRepository
{
    private readonly ConcurrentDictionary<Guid, CampusGroup> _store = new();
    private readonly object _courseLock = new();

    public InMemoryGroupRepository()
    {
        foreach (var group in SeedGroups())
            _store[group.Id] = group;
    }

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

    private static IEnumerable<CampusGroup> SeedGroups()
    {
        yield return new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Offizielle Mitteilungen",
            Description = "Verbindliche Informationen von Verwaltung, Studiengangsleitung und Hochschulleitung.",
            Type = GroupType.Official,
            Audience = "Alle Studierenden",
            OwnerLabel = "Hochschule",
            IconLabel = "OF",
            AccentColor = "#a00014",
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        };

        yield return new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Prüfungsamt und Fristen",
            Description = "Termine, Abgaben und Hinweise rund um Prüfungen und organisatorische Deadlines.",
            Type = GroupType.Official,
            Audience = "Campusweit",
            OwnerLabel = "Studienorganisation",
            IconLabel = "PF",
            AccentColor = "#6b1f2a",
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        };

        yield return CreateSeedCourseGroup("TIF25A", "Informatik", "20000000-0000-0000-0000-000000000001");
        yield return CreateSeedCourseGroup("TIF25B", "Informatik", "20000000-0000-0000-0000-000000000002");
        yield return CreateSeedCourseGroup("WWI25A", "Wirtschaftsinformatik", "20000000-0000-0000-0000-000000000003");
        yield return CreateSeedCourseGroup("TMB25A", "Maschinenbau", "20000000-0000-0000-0000-000000000004");

        yield return new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "Campusleben",
            Description = "Austausch zu Alltag, Fragen, Tipps und gemeinsamen Themen auf dem Campus.",
            Type = GroupType.Social,
            Audience = "Alle Studierenden",
            OwnerLabel = "Community",
            IconLabel = "CL",
            AccentColor = "#2563eb",
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        };

        yield return new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Name = "Sport und Events",
            Description = "Verabredungen, Hochschulsport, Campusaktionen und Freizeitveranstaltungen.",
            Type = GroupType.Social,
            Audience = "Interessierte auf dem Campus",
            OwnerLabel = "Community",
            IconLabel = "SE",
            AccentColor = "#047857",
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        };

        yield return new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            Name = "Schwarzes Brett",
            Description = "Gesuche, Angebote, Mitfahrgelegenheiten und Hinweise mit campusweiter Reichweite.",
            Type = GroupType.Social,
            Audience = "Campusweit",
            OwnerLabel = "Community mit Moderation",
            IconLabel = "SB",
            AccentColor = "#7c3aed",
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = true, IsDiscoverable = true }
        };
    }

    private static CampusGroup CreateSeedCourseGroup(string courseCode, string studyProgram, string id) => new()
    {
        Id = Guid.Parse(id),
        Name = $"Kurs {courseCode}",
        Description = $"Kursinterne Absprachen, Lernorganisation und Hinweise für {studyProgram}.",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = studyProgram,
        IconLabel = Initials(courseCode),
        AccentColor = "#e2001a",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
    };

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
