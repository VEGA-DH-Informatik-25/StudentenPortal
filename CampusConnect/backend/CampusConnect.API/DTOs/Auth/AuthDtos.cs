namespace CampusConnect.API.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName, string StudyProgram, int Semester, string Course);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string DisplayName, string StudyProgram, int Semester, string Course);
public record UserProfileResponse(Guid Id, string Email, string DisplayName, string StudyProgram, int Semester, string Course, string Role, DateTime CreatedAt);
public record AuthResponse(string Token, string DisplayName, string Email, string Role, UserProfileResponse Profile);
