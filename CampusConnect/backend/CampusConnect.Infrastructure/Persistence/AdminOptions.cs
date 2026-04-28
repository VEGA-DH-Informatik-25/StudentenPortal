namespace CampusConnect.Infrastructure.Persistence;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DisplayName { get; init; } = "Campus Admin";

    public string StudyProgram { get; init; } = "Administration";

    public int Semester { get; init; } = 1;

    public string Course { get; init; } = "ADMIN";
}