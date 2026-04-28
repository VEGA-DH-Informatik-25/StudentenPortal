using CampusConnect.Application.Common;
using CampusConnect.Application.Features.Groups;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Feed;

public record CreatePostCommand(Guid AuthorId, Guid? GroupId, string Content);
public record FeedPostDto(Guid Id, string AuthorName, CampusGroupDto Group, string Content, DateTime CreatedAt);

public class FeedService(IFeedRepository feedRepo, IGroupRepository groupRepo, IUserRepository userRepo)
{
    public async Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(int page = 1, int pageSize = 20)
    {
        var posts = await feedRepo.GetAllAsync(page, pageSize);
        var result = new List<FeedPostDto>();
        foreach (var post in posts)
            result.Add(await ToDtoAsync(post));

        return result;
    }

    public async Task<Result<FeedPostDto>> CreatePostAsync(CreatePostCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result<FeedPostDto>.Failure("Inhalt darf nicht leer sein.");

        var user = await userRepo.FindByIdAsync(cmd.AuthorId);
        if (user is null)
            return Result<FeedPostDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        var group = await ResolveTargetGroupAsync(cmd.GroupId, user);
        if (group is null)
            return Result<FeedPostDto>.Failure("Bitte wähle eine gültige Gruppe aus.");

        if (user.Role == UserRole.Student && !group.Settings.AllowStudentPosts)
            return Result<FeedPostDto>.Failure("In dieser Gruppe dürfen Studierende keine Beiträge veröffentlichen.");

        var post = new FeedPost
        {
            AuthorId = cmd.AuthorId,
            AuthorName = user.DisplayName,
            GroupId = group.Id,
            Content = cmd.Content.Trim()
        };
        await feedRepo.AddAsync(post);
        return Result<FeedPostDto>.Success(new FeedPostDto(post.Id, post.AuthorName, GroupDtoMapper.ToDto(group), post.Content, post.CreatedAt));
    }

    public async Task<Result<bool>> DeletePostAsync(Guid postId, Guid userId)
    {
        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null) return Result<bool>.Failure("Beitrag nicht gefunden.");
        if (post.AuthorId != userId)
        {
            var user = await userRepo.FindByIdAsync(userId);
            if (user?.Role != UserRole.Admin)
                return Result<bool>.Failure("Keine Berechtigung.");
        }

        await feedRepo.DeleteAsync(postId);
        return Result<bool>.Success(true);
    }

    private async Task<CampusGroup?> ResolveTargetGroupAsync(Guid? groupId, User user)
    {
        if (groupId.HasValue)
            return await groupRepo.FindByIdAsync(groupId.Value);

        if (user.Role == UserRole.Admin)
        {
            var groups = await groupRepo.GetAllAsync();
            return groups.FirstOrDefault(group => group.Type == GroupType.Official);
        }

        return string.IsNullOrWhiteSpace(user.Course)
            ? null
            : await groupRepo.EnsureCourseGroupAsync(user.Course, user.StudyProgram);
    }

    private async Task<FeedPostDto> ToDtoAsync(FeedPost post)
    {
        var group = await groupRepo.FindByIdAsync(post.GroupId) ?? new CampusGroup
        {
            Id = post.GroupId,
            Name = "Unbekannte Gruppe",
            Description = "Diese Gruppe ist nicht mehr verfügbar.",
            Type = GroupType.Social,
            Audience = "Archiv",
            OwnerLabel = "CampusConnect",
            IconLabel = "?",
            AccentColor = "#5c6672",
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = false, IsDiscoverable = false }
        };

        return new FeedPostDto(post.Id, post.AuthorName, GroupDtoMapper.ToDto(group), post.Content, post.CreatedAt);
    }
}
