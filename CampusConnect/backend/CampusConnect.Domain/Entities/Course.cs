namespace CampusConnect.Domain.Entities;

public class Course
{
    public string Code { get; set; } = string.Empty;
    public string StudyProgram { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
