using CampusConnect.Application.Common;
using CampusConnect.Application.Features.Groups;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Globalization;
using System.Text;

namespace CampusConnect.Application.Features.Feed;

public record CreatePostCommand(Guid AuthorId, Guid? GroupId, string Content);
public record CreateCommentCommand(Guid PostId, Guid AuthorId, string Content);
public record ToggleReactionCommand(Guid PostId, Guid UserId, string Emoji);
public record FeedCommentDto(Guid Id, string AuthorName, string Content, DateTime CreatedAt, bool CanDelete);
public record FeedReactionDto(string Emoji, int Count, bool ReactedByCurrentUser);
public record FeedPostDto(
    Guid Id,
    string AuthorName,
    CampusGroupDto Group,
    string Content,
    DateTime CreatedAt,
    bool CanDelete,
    bool CanComment,
    IReadOnlyList<FeedCommentDto> Comments,
    IReadOnlyList<FeedReactionDto> Reactions);

public class FeedService(IFeedRepository feedRepo, IGroupRepository groupRepo, IUserRepository userRepo)
{
    public async Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(Guid currentUserId, int page = 1, int pageSize = 20)
    {
        await SyncCourseGroupAssignmentsAsync();
        var posts = await feedRepo.GetAllAsync(page, pageSize);
        var currentUser = await userRepo.FindByIdAsync(currentUserId);
        var result = new List<FeedPostDto>();
        foreach (var post in posts)
        {
            var group = await ResolvePostGroupAsync(post);
            if (!GroupDtoMapper.CanReadPosts(currentUser, group))
                continue;

            result.Add(ToDto(post, group, currentUserId, currentUser));
        }

        return result;
    }

    public async Task<Result<FeedPostDto>> CreatePostAsync(CreatePostCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result<FeedPostDto>.Failure("Inhalt darf nicht leer sein.");

        var user = await userRepo.FindByIdAsync(cmd.AuthorId);
        if (user is null)
            return Result<FeedPostDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolveTargetGroupAsync(cmd.GroupId, user);
        if (group is null)
            return Result<FeedPostDto>.Failure("Bitte wähle eine gültige Gruppe aus.");

        if (!GroupDtoMapper.CanPost(user, group))
        {
            if (GroupDtoMapper.IsAssigned(user, group) && !GroupDtoMapper.CanWrite(user, group))
                return Result<FeedPostDto>.Failure("Du hast in dieser Gruppe nur Leserechte.");

            if (GroupDtoMapper.IsAssigned(user, group) && user.Role == UserRole.Student && !group.Settings.AllowStudentPosts)
                return Result<FeedPostDto>.Failure("In dieser Gruppe dürfen Studierende keine Beiträge veröffentlichen.");

            return Result<FeedPostDto>.Failure("Du kannst nur in Gruppen posten, denen du zugewiesen bist.");
        }

        var post = new FeedPost
        {
            AuthorId = cmd.AuthorId,
            AuthorName = user.DisplayName,
            GroupId = group.Id,
            Content = cmd.Content.Trim()
        };
        await feedRepo.AddAsync(post);
        return Result<FeedPostDto>.Success(ToDto(post, group, cmd.AuthorId, user));
    }

    public async Task<Result<FeedPostDto>> AddCommentAsync(CreateCommentCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result<FeedPostDto>.Failure("Kommentar darf nicht leer sein.");

        var user = await userRepo.FindByIdAsync(cmd.AuthorId);
        if (user is null)
            return Result<FeedPostDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        var post = await feedRepo.FindByIdAsync(cmd.PostId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Beitrag nicht gefunden.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolvePostGroupAsync(post);
        if (!CanParticipate(user, group))
            return Result<FeedPostDto>.Failure("Keine Berechtigung.");

        if (!group.Settings.AllowComments)
            return Result<FeedPostDto>.Failure("Kommentare sind in dieser Gruppe geschlossen.");

        var comment = new FeedComment
        {
            AuthorId = cmd.AuthorId,
            AuthorName = user.DisplayName,
            Content = cmd.Content.Trim()
        };

        var updatedPost = await feedRepo.AddCommentAsync(cmd.PostId, comment);
        return updatedPost is null
            ? Result<FeedPostDto>.Failure("Beitrag nicht gefunden.")
            : Result<FeedPostDto>.Success(ToDto(updatedPost, group, cmd.AuthorId, user));
    }

    public async Task<Result<FeedPostDto>> DeleteCommentAsync(Guid postId, Guid commentId, Guid userId)
    {
        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Beitrag nicht gefunden.");

        var comment = post.Comments.FirstOrDefault(item => item.Id == commentId);
        if (comment is null)
            return Result<FeedPostDto>.Failure("Kommentar nicht gefunden.");

        var currentUser = await userRepo.FindByIdAsync(userId);
        if (currentUser is null)
            return Result<FeedPostDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        if (comment.AuthorId != userId && currentUser.Role != UserRole.Admin)
            return Result<FeedPostDto>.Failure("Keine Berechtigung.");

        var updatedPost = await feedRepo.DeleteCommentAsync(postId, commentId);
        var group = await ResolvePostGroupAsync(post);
        return updatedPost is null
            ? Result<FeedPostDto>.Failure("Beitrag nicht gefunden.")
            : Result<FeedPostDto>.Success(ToDto(updatedPost, group, userId, currentUser));
    }

    public async Task<Result<FeedPostDto>> ToggleReactionAsync(ToggleReactionCommand cmd)
    {
        var emoji = cmd.Emoji.Trim();
        if (!IsSupportedEmoji(emoji))
            return Result<FeedPostDto>.Failure("Bitte wähle ein gültiges Emoji aus.");

        var user = await userRepo.FindByIdAsync(cmd.UserId);
        if (user is null)
            return Result<FeedPostDto>.Failure("Benutzerprofil wurde nicht gefunden.");

        var post = await feedRepo.FindByIdAsync(cmd.PostId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Beitrag nicht gefunden.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolvePostGroupAsync(post);
        if (!CanParticipate(user, group))
            return Result<FeedPostDto>.Failure("Keine Berechtigung.");

        var updatedPost = await feedRepo.ToggleReactionAsync(cmd.PostId, emoji, cmd.UserId);
        return updatedPost is null
            ? Result<FeedPostDto>.Failure("Beitrag nicht gefunden.")
            : Result<FeedPostDto>.Success(ToDto(updatedPost, group, cmd.UserId, user));
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
        {
            var group = await groupRepo.FindByIdAsync(groupId.Value);
            return group is not null && GroupDtoMapper.CanView(user, group) ? group : null;
        }

        if (user.Role == UserRole.Admin)
        {
            var groups = await groupRepo.GetAllAsync();
            return groups.FirstOrDefault(group => group.Type == GroupType.Official);
        }

        if (string.IsNullOrWhiteSpace(user.Course))
            return null;

        var courseGroup = await groupRepo.EnsureCourseGroupAsync(user.Course, user.StudyProgram);
        await SyncCourseGroupAssignmentsAsync();
        return await groupRepo.FindByIdAsync(courseGroup.Id) ?? courseGroup;
    }

    private async Task<CampusGroup> ResolvePostGroupAsync(FeedPost post) => await groupRepo.FindByIdAsync(post.GroupId) ?? MissingGroup(post.GroupId);

    private static FeedPostDto ToDto(FeedPost post, CampusGroup group, Guid currentUserId, User? currentUser = null)
    {
        var canModerate = currentUser?.Role == UserRole.Admin;
        var comments = post.Comments
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new FeedCommentDto(
                comment.Id,
                comment.AuthorName,
                comment.Content,
                comment.CreatedAt,
                comment.AuthorId == currentUserId || canModerate))
            .ToList();
        var reactions = post.Reactions
            .Where(reaction => reaction.UserIds.Count > 0)
            .OrderByDescending(reaction => reaction.UserIds.Count)
            .ThenBy(reaction => reaction.Emoji, StringComparer.Ordinal)
            .Select(reaction => new FeedReactionDto(reaction.Emoji, reaction.UserIds.Count, reaction.UserIds.Contains(currentUserId)))
            .ToList();

        return new FeedPostDto(
            post.Id,
            post.AuthorName,
            GroupDtoMapper.ToDto(group, currentUser),
            post.Content,
            post.CreatedAt,
            post.AuthorId == currentUserId || canModerate,
            group.Settings.AllowComments && currentUser is not null && CanParticipate(currentUser, group),
            comments,
            reactions);
    }

    private async Task SyncCourseGroupAssignmentsAsync()
    {
        var users = await userRepo.ListAsync();
        var groups = await groupRepo.GetAllAsync();
        foreach (var group in groups.Where(group => group.Type == GroupType.Course && !string.IsNullOrWhiteSpace(group.CourseCode)))
        {
            var assignedUserIds = users
                .Where(user => string.Equals(user.Course, group.CourseCode, StringComparison.OrdinalIgnoreCase))
                .Select(user => user.Id)
                .ToList();

            await groupRepo.SyncCourseAssignmentsAsync(group.CourseCode!, assignedUserIds);
        }
    }

    private static bool CanParticipate(User user, CampusGroup group) => GroupDtoMapper.CanWrite(user, group);

    private static bool IsSupportedEmoji(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji) || emoji.Length > 32)
            return false;

        var hasEmojiSymbol = false;
        var hasKeycapMark = false;
        foreach (var rune in emoji.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            hasEmojiSymbol |= category is UnicodeCategory.OtherSymbol or UnicodeCategory.ModifierSymbol;
            hasKeycapMark |= rune.Value == 0x20E3;

            var allowed = category is
                UnicodeCategory.OtherSymbol or
                UnicodeCategory.ModifierSymbol or
                UnicodeCategory.NonSpacingMark or
                UnicodeCategory.EnclosingMark or
                UnicodeCategory.Format or
                UnicodeCategory.DecimalDigitNumber or
                UnicodeCategory.OtherPunctuation;

            if (!allowed)
                return false;
        }

        return hasEmojiSymbol || hasKeycapMark;
    }

    private static CampusGroup MissingGroup(Guid groupId) => new()
    {
        Id = groupId,
        Name = "Unbekannte Gruppe",
        Description = "Diese Gruppe ist nicht mehr verfügbar.",
        Type = GroupType.Social,
        Audience = "Archiv",
        OwnerLabel = "CampusConnect",
        IconLabel = "?",
        AccentColor = "#5c6672",
        Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = false, IsDiscoverable = false }
    };
}
