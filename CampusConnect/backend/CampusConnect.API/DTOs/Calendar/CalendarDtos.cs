namespace CampusConnect.API.DTOs.Calendar;

public record AddExamRequest(string ModuleName, DateTime ExamDate, string? Location, string? Notes);
