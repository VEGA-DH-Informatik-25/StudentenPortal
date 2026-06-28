using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CampusConnect.Infrastructure.Repositories;

public class InMemoryFeedRepository : IFeedRepository
{
    private readonly ConcurrentDictionary<Guid, FeedPost> _store = new();
    private readonly object _syncRoot = new();

    public Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize)
    {
        lock (_syncRoot)
        {
            var posts = _store.Values
                .OrderByDescending(post => post.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<FeedPost>>(posts);
        }
    }

    public Task<FeedPost?> FindByIdAsync(Guid id)
    {
        lock (_syncRoot)
        {
            _store.TryGetValue(id, out var post);
            return Task.FromResult(post is null ? null : Clone(post));
        }
    }

    public Task<IReadOnlyList<FeedPost>> GetPublishedAsync(int page, int pageSize)
    {
        lock (_syncRoot)
        {
            var posts = _store.Values
                .Where(post => post.Status == FeedPostStatus.Published)
                .OrderByDescending(post => post.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<FeedPost>>(posts);
        }
    }

    public Task<IReadOnlyList<FeedPost>> GetByGroupAsync(Guid groupId)
    {
        lock (_syncRoot)
        {
            var posts = _store.Values
                .Where(post => post.GroupId == groupId)
                .OrderByDescending(post => post.CreatedAt)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<FeedPost>>(posts);
        }
    }

    public Task AddAsync(FeedPost post)
    {
        lock (_syncRoot)
        {
            _store[post.Id] = Clone(post);
        }

        return Task.CompletedTask;
    }

    public Task<FeedPost?> SetStatusAsync(Guid id, FeedPostStatus status)
    {
        lock (_syncRoot)
        {
            if (!_store.TryGetValue(id, out var post))
                return Task.FromResult<FeedPost?>(null);

            post.Status = status;
            return Task.FromResult<FeedPost?>(Clone(post));
        }
    }

    public Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment)
    {
        lock (_syncRoot)
        {
            if (!_store.TryGetValue(postId, out var post))
                return Task.FromResult<FeedPost?>(null);

            post.Comments.Add(comment);
            return Task.FromResult<FeedPost?>(Clone(post));
        }
    }

    public Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId)
    {
        lock (_syncRoot)
        {
            if (!_store.TryGetValue(postId, out var post))
                return Task.FromResult<FeedPost?>(null);

            post.Comments.RemoveAll(comment => comment.Id == commentId);
            return Task.FromResult<FeedPost?>(Clone(post));
        }
    }

    public Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId)
    {
        lock (_syncRoot)
        {
            if (!_store.TryGetValue(postId, out var post))
                return Task.FromResult<FeedPost?>(null);

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

            return Task.FromResult<FeedPost?>(Clone(post));
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_syncRoot)
        {
            _store.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    public Task DeleteByGroupAsync(Guid groupId)
    {
        lock (_syncRoot)
        {
            foreach (var id in _store.Values.Where(post => post.GroupId == groupId).Select(post => post.Id).ToList())
                _store.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    private static FeedPost Clone(FeedPost post) => new()
    {
        Id = post.Id,
        AuthorId = post.AuthorId,
        GroupId = post.GroupId,
        AuthorName = post.AuthorName,
        Content = post.Content,
        Translations = Clone(post.Translations),
        Attachments = post.Attachments.Select(Clone).ToList(),
        Status = post.Status,
        AllowComments = post.AllowComments,
        CreatedAt = post.CreatedAt,
        Comments = post.Comments.Select(Clone).ToList(),
        Reactions = post.Reactions.Select(Clone).ToList()
    };

    private static FeedPostTranslations? Clone(FeedPostTranslations? translations) =>
        translations is null
            ? null
            : new FeedPostTranslations
            {
                De = translations.De,
                En = translations.En,
                Fr = translations.Fr
            };

    private static FeedAttachment Clone(FeedAttachment attachment) => new()
    {
        Id = attachment.Id,
        OriginalFileName = attachment.OriginalFileName,
        StoredFileName = attachment.StoredFileName,
        ContentType = attachment.ContentType,
        SizeBytes = attachment.SizeBytes,
        IsImage = attachment.IsImage,
        CreatedAt = attachment.CreatedAt
    };

    private static FeedComment Clone(FeedComment comment) => new()
    {
        Id = comment.Id,
        AuthorId = comment.AuthorId,
        AuthorName = comment.AuthorName,
        Content = comment.Content,
        CreatedAt = comment.CreatedAt
    };

    private static FeedReaction Clone(FeedReaction reaction) => new()
    {
        Emoji = reaction.Emoji,
        UserIds = reaction.UserIds.ToHashSet()
    };
}
