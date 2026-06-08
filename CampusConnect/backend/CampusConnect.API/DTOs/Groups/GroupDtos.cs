namespace CampusConnect.API.DTOs.Groups;

public record CreateGroupRequest(
	string Name,
	string Description,
	string Audience,
	bool AllowStudentPosts = true,
	bool AllowComments = true,
	bool RequiresApproval = false,
	bool IsDiscoverable = true,
	string Type = "Social",
	string? CourseCode = null);
public record UpdateGroupSettingsRequest(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable);
public record AddGroupMembersRequest(IReadOnlyList<Guid> UserIds);
public record AddGroupCourseRequest(string CourseCode);
public record SetGroupMemberRoleRequest(string Role);
