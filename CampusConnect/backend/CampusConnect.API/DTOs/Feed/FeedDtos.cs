namespace CampusConnect.API.DTOs.Feed;

public record CreatePostRequest(string Content, Guid? GroupId);

public record CreateCommentRequest(string Content);

public record ToggleReactionRequest(string Emoji);
