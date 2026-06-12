using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Interfaces;

public interface IFeedRepository
{
    Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize);
    async Task<IReadOnlyList<FeedPost>> GetPublishedAsync(int page, int pageSize) =>
        (await GetAllAsync(page, pageSize)).Where(post => post.Status == FeedPostStatus.Published).ToList();
    Task<IReadOnlyList<FeedPost>> GetByGroupAsync(Guid groupId) =>
        throw new NotSupportedException();
    Task<FeedPost?> FindByIdAsync(Guid id);
    Task AddAsync(FeedPost post);
    Task<FeedPost?> SetStatusAsync(Guid id, FeedPostStatus status) =>
        throw new NotSupportedException();
    Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment);
    Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId);
    Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId);
    Task DeleteAsync(Guid id);
    Task DeleteByGroupAsync(Guid groupId) =>
        throw new NotSupportedException();
}
