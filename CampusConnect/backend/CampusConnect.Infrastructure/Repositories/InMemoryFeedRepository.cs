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

    public Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment)
    {
        if (!_store.TryGetValue(postId, out var post))
            return Task.FromResult<FeedPost?>(null);

        lock (post)
        {
            post.Comments.Add(comment);
        }

        return Task.FromResult<FeedPost?>(post);
    }

    public Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId)
    {
        if (!_store.TryGetValue(postId, out var post))
            return Task.FromResult<FeedPost?>(null);

        lock (post)
        {
            post.Comments.RemoveAll(comment => comment.Id == commentId);
        }

        return Task.FromResult<FeedPost?>(post);
    }

    public Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId)
    {
        if (!_store.TryGetValue(postId, out var post))
            return Task.FromResult<FeedPost?>(null);

        lock (post)
        {
            var reaction = post.Reactions.FirstOrDefault(item => item.Emoji == emoji);
            if (reaction is null)
            {
                post.Reactions.Add(new FeedReaction { Emoji = emoji, UserIds = [userId] });
            }
            else if (!reaction.UserIds.Add(userId))
            {
                reaction.UserIds.Remove(userId);
                if (reaction.UserIds.Count == 0)
                    post.Reactions.Remove(reaction);
            }
        }

        return Task.FromResult<FeedPost?>(post);
    }

    public Task DeleteAsync(Guid id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
