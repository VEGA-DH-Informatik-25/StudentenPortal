namespace CampusConnect.API.DTOs.Feed;

public record CreatePostRequest(string Content, Guid? GroupId);
