namespace CampusConnect.Infrastructure.Persistence;

public sealed class DemoDataOptions
{
    public const string SectionName = "DemoData";

    public bool Enabled { get; set; } = true;

    public string Password { get; set; } = "CampusDemo2026!";

    public List<DemoCourseOptions> Courses { get; set; } = [];

    public List<string> TechnicalCoursePrefixes { get; set; } = [];
}

public sealed class DemoCourseOptions
{
    public string Code { get; set; } = string.Empty;

    public string StudyProgram { get; set; } = string.Empty;

    public int? Semester { get; set; } = 1;
}
