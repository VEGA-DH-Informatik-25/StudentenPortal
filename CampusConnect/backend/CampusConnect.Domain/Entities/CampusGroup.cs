using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Entities;

public class CampusGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GroupType Type { get; set; }
    public string Audience { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string OwnerLabel { get; set; } = string.Empty;
    public string IconLabel { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#e2001a";
    public GroupSettings Settings { get; set; } = new();
    public HashSet<Guid> AssignedUserIds { get; set; } = [];

    /// <summary>
    /// Group role of each assigned member. The owner (<see cref="OwnerUserId"/>) is always treated as
    /// <see cref="GroupRole.Owner"/> and is not stored here. Assigned members without an explicit entry
    /// default to <see cref="GroupRole.Member"/>.
    /// </summary>
    public Dictionary<Guid, GroupRole> MemberRoles { get; set; } = [];
}
