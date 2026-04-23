namespace CampusConnect.Domain.Entities;

public class Grade
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Ects { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
