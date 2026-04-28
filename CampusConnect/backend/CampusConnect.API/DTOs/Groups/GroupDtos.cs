namespace CampusConnect.API.DTOs.Groups;

public record CreateGroupRequest(string Name, string Description, string Audience);
public record UpdateGroupSettingsRequest(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record UpdateGroupAssignmentsRequest(IReadOnlyList<Guid> UserIds);
