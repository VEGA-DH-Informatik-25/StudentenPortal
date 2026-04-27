using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Feed;

public record CreatePostCommand(Guid AuthorId, string AuthorName, string Content);
public record FeedPostDto(Guid Id, string AuthorName, string Content, DateTime CreatedAt);

public class FeedService(IFeedRepository feedRepo)
{
    public async Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(int page = 1, int pageSize = 20)
    {
        var posts = await feedRepo.GetAllAsync(page, pageSize);
        return posts.Select(p => new FeedPostDto(p.Id, p.AuthorName, p.Content, p.CreatedAt)).ToList();
    }

    public async Task<Result<FeedPostDto>> CreatePostAsync(CreatePostCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result<FeedPostDto>.Failure("Inhalt darf nicht leer sein.");

        var post = new FeedPost
        {
            AuthorId = cmd.AuthorId,
            AuthorName = cmd.AuthorName,
            Content = cmd.Content.Trim()
        };
        await feedRepo.AddAsync(post);
        return Result<FeedPostDto>.Success(new FeedPostDto(post.Id, post.AuthorName, post.Content, post.CreatedAt));
    }

    public async Task<Result<bool>> DeletePostAsync(Guid postId, Guid userId)
    {
        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null) return Result<bool>.Failure("Beitrag nicht gefunden.");
        if (post.AuthorId != userId) return Result<bool>.Failure("Keine Berechtigung.");
        await feedRepo.DeleteAsync(postId);
        return Result<bool>.Success(true);
    }
}
