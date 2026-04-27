using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IFeedRepository
{
    Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize);
    Task<FeedPost?> FindByIdAsync(Guid id);
    Task AddAsync(FeedPost post);
    Task DeleteAsync(Guid id);
}
