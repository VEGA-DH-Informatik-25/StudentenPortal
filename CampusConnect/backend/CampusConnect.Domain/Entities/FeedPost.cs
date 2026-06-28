using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Entities;

public class FeedPost
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public Guid GroupId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public FeedPostTranslations? Translations { get; set; }
    public List<FeedAttachment> Attachments { get; set; } = [];
    public FeedPostStatus Status { get; set; } = FeedPostStatus.Published;
    public bool AllowComments { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<FeedComment> Comments { get; set; } = [];
    public List<FeedReaction> Reactions { get; set; } = [];
}

public class FeedPostTranslations
{
    public string De { get; set; } = string.Empty;
    public string En { get; set; } = string.Empty;
    public string Fr { get; set; } = string.Empty;
}

public class FeedAttachment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public bool IsImage { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class FeedComment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class FeedReaction
{
    public string Emoji { get; set; } = string.Empty;
    public HashSet<Guid> UserIds { get; set; } = [];
}
