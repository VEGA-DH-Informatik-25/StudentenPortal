using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryFeedRepository : IFeedRepository
{
    private readonly ConcurrentDictionary<Guid, FeedPost> _store = new();

    public Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize)
    {
        var posts = _store.Values
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<FeedPost>>(posts);
    }

    public Task<FeedPost?> FindByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var post);
        return Task.FromResult(post);
    }

    public Task AddAsync(FeedPost post)
    {
        _store[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
