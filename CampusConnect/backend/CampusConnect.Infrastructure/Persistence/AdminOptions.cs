namespace CampusConnect.Infrastructure.Persistence;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Email { get; init; } = "admin@dhbw-loerrach.de";

    public string Password { get; init; } = "Admin123!";

    public string DisplayName { get; init; } = "Campus Admin";

    public string StudyProgram { get; init; } = "Administration";

    public int Semester { get; init; } = 1;

    public string Course { get; init; } = "ADMIN";
}