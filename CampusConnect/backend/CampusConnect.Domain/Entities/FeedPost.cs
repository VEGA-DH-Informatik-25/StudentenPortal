namespace CampusConnect.Domain.Entities;

public class FeedPost
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public Guid GroupId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<FeedComment> Comments { get; set; } = [];
    public List<FeedReaction> Reactions { get; set; } = [];
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
