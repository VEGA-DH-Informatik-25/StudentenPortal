namespace CampusConnect.Domain.Entities;

public class ExamEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
