namespace CampusConnect.Infrastructure.Services;

public sealed class FeedAttachmentStorageOptions
{
    public const string SectionName = "FeedAttachments";

    public string UploadPath { get; set; } = Path.Combine("App_Data", "feed-uploads");
}
