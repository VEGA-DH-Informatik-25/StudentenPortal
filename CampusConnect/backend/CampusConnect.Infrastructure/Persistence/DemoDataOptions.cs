namespace CampusConnect.Infrastructure.Persistence;

public sealed class DemoDataOptions
{
    public const string SectionName = "DemoData";

    public bool Enabled { get; init; } = true;

    public string Password { get; init; } = "CampusDemo2026!";
}