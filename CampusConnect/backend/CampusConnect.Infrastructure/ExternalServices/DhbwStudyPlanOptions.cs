namespace CampusConnect.Infrastructure.ExternalServices;

public sealed class DhbwStudyPlanOptions
{
    public const string SectionName = "DhbwStudyPlans";

    public string CampusCode { get; set; } = "LÖ";
    public int CacheMinutes { get; set; } = 360;
    public List<string> IndexUrls { get; set; } =
    [
        "https://www.dhbw.de/fileadmin/user/public/SP/Studienbereich_Technik.htm",
        "https://www.dhbw.de/fileadmin/user/public/SP/Studienbereich_Wirtschaft.htm",
        "https://www.dhbw.de/fileadmin/user/public/SP/Studienbereich_Gesundheit.htm"
    ];
}