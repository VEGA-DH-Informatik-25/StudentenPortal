namespace CampusConnect.API.DTOs.Groups;

public record CreateGroupRequest(
	string Name,
	string Description,
	string Audience,
	bool AllowStudentPosts = true,
	bool AllowComments = true,
	bool RequiresApproval = false,
	bool IsDiscoverable = true);
public record UpdateGroupSettingsRequest(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record UpdateGroupAssignmentsRequest(IReadOnlyList<Guid> UserIds);
public record UpdateGroupMemberPermissionRequest(Guid UserId, string Permission);
public record UpdateGroupMemberPermissionsRequest(IReadOnlyList<UpdateGroupMemberPermissionRequest> Permissions);
