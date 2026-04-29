namespace CampusConnect.API.DTOs.Courses;

public record CreateCourseRequest(string Code, string StudyProgram, int Semester);
