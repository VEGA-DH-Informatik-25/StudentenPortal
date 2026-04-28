namespace CampusConnect.Infrastructure.ExternalServices;

public sealed class MensaOptions
{
    public const string SectionName = "Mensa";

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://www.swfr.de/apispeiseplan";

    public string OrtId { get; init; } = "677";

    public int Days { get; init; } = 5;
}