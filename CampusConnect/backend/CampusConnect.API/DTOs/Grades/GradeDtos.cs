namespace CampusConnect.API.DTOs.Grades;

public record AddGradeRequest(string? ModuleName, decimal Value, int? Ects, string? ModuleCode);
