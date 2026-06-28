namespace CampusConnect.API.DTOs.Feed;

public record CreatePostRequest(string Content, Guid? GroupId, bool AllowComments = true, CreatePostTranslationsRequest? Translations = null);

public record CreatePostTranslationsRequest(string? De, string? En, string? Fr);

public record CreateCommentRequest(string Content);

public record ToggleReactionRequest(string Emoji);
