using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Entities;

public class GroupSettings
{
    public bool AllowStudentPosts { get; set; } = true;
    public bool AllowComments { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public bool IsDiscoverable { get; set; } = true;
    public GroupJoinRule JoinRule { get; set; } = GroupJoinRule.Open;
}
