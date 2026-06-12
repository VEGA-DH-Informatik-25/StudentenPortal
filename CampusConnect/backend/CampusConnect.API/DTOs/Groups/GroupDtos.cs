namespace CampusConnect.API.DTOs.Groups;

public record CreateGroupRequest(
	string Name,
	string Description,
	string Audience,
	bool AllowStudentPosts = true,
	bool AllowComments = true,
	bool RequiresApproval = false,
	bool IsDiscoverable = true,
	string Type = "Campus",
	string? CourseCode = null,
	string JoinRule = "Open",
	string? OfficialCategory = null);
public record UpdateGroupSettingsRequest(bool AllowStudentPosts, bool AllowComments, bool RequiresApproval, bool IsDiscoverable, string JoinRule = "Open");
public record AddGroupMembersRequest(IReadOnlyList<Guid> UserIds);
public record InviteGroupMembersRequest(IReadOnlyList<Guid> UserIds);
public record AddGroupCourseRequest(string CourseCode);
public record SetGroupMemberRoleRequest(string Role);
public record LeaveGroupRequest(Guid? NewOwnerUserId = null);
