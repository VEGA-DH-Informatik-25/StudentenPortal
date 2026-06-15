namespace CampusConnect.API.DTOs.Admin;

public record CreateAdminUserRequest(string FirstName, string LastName, string Email, string Role, string CourseCode, string InitialPassword, bool IsActive = true);
public record UpdateAdminUserRequest(string DisplayName, string Email, string Role, string CourseCode, bool IsActive = true);
public record UpdateUserRoleRequest(string Role);
public record UpdateUserCourseRequest(string CourseCode);
public record UpdateUserStatusRequest(bool IsActive);
