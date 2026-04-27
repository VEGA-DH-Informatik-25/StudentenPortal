namespace CampusConnect.API.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName, string StudyProgram, int Semester, string Course);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string DisplayName, string Email, string Role);
