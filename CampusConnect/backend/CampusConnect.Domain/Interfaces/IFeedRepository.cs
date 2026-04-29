using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IFeedRepository
{
    Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize);
    Task<FeedPost?> FindByIdAsync(Guid id);
    Task AddAsync(FeedPost post);
    Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment);
    Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId);
    Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId);
    Task DeleteAsync(Guid id);
}
