using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Repositories;

public sealed class EntityFeedRepository(CampusConnectDbContext dbContext) : IFeedRepository
{
    public async Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize) =>
        await dbContext.FeedPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(post => Clone(post))
            .ToListAsync();

    public async Task<IReadOnlyList<FeedPost>> GetPublishedAsync(int page, int pageSize) =>
        await dbContext.FeedPosts
            .AsNoTracking()
            .Where(post => post.Status == FeedPostStatus.Published)
            .OrderByDescending(post => post.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(post => Clone(post))
            .ToListAsync();

    public async Task<IReadOnlyList<FeedPost>> GetByGroupAsync(Guid groupId) =>
        await dbContext.FeedPosts
            .AsNoTracking()
            .Where(post => post.GroupId == groupId)
            .OrderByDescending(post => post.CreatedAt)
            .Select(post => Clone(post))
            .ToListAsync();

    public async Task<FeedPost?> FindByIdAsync(Guid id)
    {
        var post = await dbContext.FeedPosts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return post is null ? null : Clone(post);
    }

    public async Task AddAsync(FeedPost post)
    {
        var existing = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == post.Id);
        if (existing is null)
        {
            dbContext.FeedPosts.Add(Clone(post));
        }
        else
        {
            Copy(post, existing);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<FeedPost?> SetStatusAsync(Guid id, FeedPostStatus status)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == id);
        if (post is null)
            return null;

        post.Status = status;
        await dbContext.SaveChangesAsync();
        return Clone(post);
    }

    public async Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == postId);
        if (post is null)
            return null;

        post.Comments = post.Comments.Select(Clone).Append(Clone(comment)).ToList();
        await dbContext.SaveChangesAsync();
        return Clone(post);
    }

    public async Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == postId);
        if (post is null)
            return null;

        post.Comments = post.Comments.Where(comment => comment.Id != commentId).Select(Clone).ToList();
        await dbContext.SaveChangesAsync();
        return Clone(post);
    }

    public async Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == postId);
        if (post is null)
            return null;

        var reactions = post.Reactions.Select(Clone).ToList();
        var reaction = reactions.FirstOrDefault(item => item.Emoji == emoji);
        if (reaction is null)
        {
            reactions.Add(new FeedReaction { Emoji = emoji, UserIds = [userId] });
        }
        else if (!reaction.UserIds.Add(userId))
        {
            reaction.UserIds.Remove(userId);
            if (reaction.UserIds.Count == 0)
                reactions.Remove(reaction);
        }

        post.Reactions = reactions;
        await dbContext.SaveChangesAsync();
        return Clone(post);
    }

    public async Task DeleteAsync(Guid id)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(item => item.Id == id);
        if (post is null)
            return;

        dbContext.FeedPosts.Remove(post);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteByGroupAsync(Guid groupId)
    {
        var posts = await dbContext.FeedPosts.Where(post => post.GroupId == groupId).ToListAsync();
        if (posts.Count == 0)
            return;

        dbContext.FeedPosts.RemoveRange(posts);
        await dbContext.SaveChangesAsync();
    }

    private static void Copy(FeedPost source, FeedPost target)
    {
        target.AuthorId = source.AuthorId;
        target.GroupId = source.GroupId;
        target.AuthorName = source.AuthorName;
        target.Content = source.Content;
        target.Translations = Clone(source.Translations);
        target.Attachments = source.Attachments.Select(Clone).ToList();
        target.Status = source.Status;
        target.AllowComments = source.AllowComments;
        target.Comments = source.Comments.Select(Clone).ToList();
        target.Reactions = source.Reactions.Select(Clone).ToList();
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
