using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Contacts;
using CampusConnect.Application.Features.Groups;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Globalization;
using System.Text;

namespace CampusConnect.Application.Features.Feed;

public record CreatePostCommand(
    Guid AuthorId,
    Guid? GroupId,
    string Content,
    bool AllowComments = true,
    FeedPostTranslationInput? Translations = null,
    IReadOnlyList<CreatePostAttachment>? Attachments = null);
public record FeedPostTranslationInput(string? De, string? En, string? Fr);
public record CreatePostAttachment(string FileName, string ContentType, long SizeBytes, Stream Content);
public record CreateCommentCommand(Guid PostId, Guid AuthorId, string Content);
public record ToggleReactionCommand(Guid PostId, Guid UserId, string Emoji);
public record FeedPostTranslationDto(string De, string En, string Fr);
public record FeedAttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, bool IsImage, string DownloadUrl);
public record FeedAttachmentDownloadResult(FeedAttachment Attachment, Stream Content);
public record FeedCommentDto(Guid Id, string AuthorName, ContactProfileDto? Author, string Content, DateTime CreatedAt, bool CanDelete);
public record FeedReactionDto(string Emoji, int Count, bool ReactedByCurrentUser);
public record FeedPostDto(
    Guid Id,
    string AuthorName,
    ContactProfileDto? Author,
    CampusGroupDto Group,
    string Content,
    FeedPostTranslationDto? Translations,
    IReadOnlyList<FeedAttachmentDto> Attachments,
    DateTime CreatedAt,
    string Status,
    bool AllowComments,
    bool CanDelete,
    bool CanComment,
    IReadOnlyList<FeedCommentDto> Comments,
    IReadOnlyList<FeedReactionDto> Reactions);

public class FeedService(IFeedRepository feedRepo, IGroupRepository groupRepo, IUserRepository userRepo, IFeedAttachmentStorage? attachmentStorage = null)
{
    public const int MaxAttachmentCount = 5;
    public const long MaxAttachmentBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".txt", ".csv",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    public async Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(Guid currentUserId, int page = 1, int pageSize = 20)
    {
        await SyncCourseGroupAssignmentsAsync();
        var posts = await feedRepo.GetPublishedAsync(page, pageSize);
        var users = await userRepo.ListAsync();
        var usersById = users.ToDictionary(user => user.Id);
        usersById.TryGetValue(currentUserId, out var currentUser);
        var result = new List<FeedPostDto>();
        foreach (var post in posts)
        {
            var group = await ResolvePostGroupAsync(post);
            if (!GroupDtoMapper.CanReadPosts(currentUser, group))
                continue;

            result.Add(ToDto(post, group, currentUserId, currentUser, usersById));
        }

        return result;
    }

    public async Task<Result<FeedPostDto>> CreatePostAsync(CreatePostCommand cmd)
    {
        var translationsResult = NormalizeTranslations(cmd.Translations);
        if (!translationsResult.IsSuccess)
            return Result<FeedPostDto>.Failure(translationsResult.Error ?? "Content cannot be empty.");

        var content = translationsResult.Value?.De ?? cmd.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
            return Result<FeedPostDto>.Failure("Content cannot be empty.");

        var attachmentValidationError = ValidateAttachments(cmd.Attachments ?? []);
        if (attachmentValidationError is not null)
            return Result<FeedPostDto>.Failure(attachmentValidationError);

        var user = await userRepo.FindByIdAsync(cmd.AuthorId);
        if (user is null)
            return Result<FeedPostDto>.Failure("User profile was not found.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolveTargetGroupAsync(cmd.GroupId, user);
        if (group is null)
            return Result<FeedPostDto>.Failure("Choose a valid group.");

        if (!GroupDtoMapper.CanPost(user, group))
        {
            if (GroupDtoMapper.IsAssigned(user, group) && !group.Settings.AllowStudentPosts)
                return Result<FeedPostDto>.Failure("Members are not allowed to publish posts in this group.");

            return Result<FeedPostDto>.Failure("You can only post in groups assigned to you.");
        }

        var post = new FeedPost
        {
            AuthorId = cmd.AuthorId,
            AuthorName = user.DisplayName,
            GroupId = group.Id,
            Content = content,
            Translations = translationsResult.Value,
            Status = group.Settings.RequiresApproval && GroupDtoMapper.GroupRoleFor(user.Id, group) == GroupRole.Member
                ? FeedPostStatus.Pending
                : FeedPostStatus.Published,
            AllowComments = group.Settings.AllowComments && cmd.AllowComments
        };

        var savedAttachments = new List<FeedAttachment>();
        try
        {
            foreach (var attachment in cmd.Attachments ?? [])
            {
                if (attachmentStorage is null)
                    return Result<FeedPostDto>.Failure("Attachment storage is not available.");

                savedAttachments.Add(await attachmentStorage.SaveAsync(
                    attachment.Content,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.SizeBytes));
            }
        }
        catch
        {
            if (attachmentStorage is not null)
                await attachmentStorage.DeleteManyAsync(savedAttachments);
            throw;
        }

        post.Attachments = savedAttachments;
        await feedRepo.AddAsync(post);
        return Result<FeedPostDto>.Success(await ToDtoAsync(post, group, cmd.AuthorId, user));
    }

    public async Task<Result<FeedPostDto>> AddCommentAsync(CreateCommentCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result<FeedPostDto>.Failure("Comment cannot be empty.");

        var user = await userRepo.FindByIdAsync(cmd.AuthorId);
        if (user is null)
            return Result<FeedPostDto>.Failure("User profile was not found.");

        var post = await feedRepo.FindByIdAsync(cmd.PostId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolvePostGroupAsync(post);
        if (!CanParticipate(user, group))
            return Result<FeedPostDto>.Failure("Permission denied.");

        if (post.Status != FeedPostStatus.Published)
            return Result<FeedPostDto>.Failure("This post is waiting for approval.");

        if (!group.Settings.AllowComments || !post.AllowComments)
            return Result<FeedPostDto>.Failure("Comments are closed in this group.");

        var comment = new FeedComment
        {
            AuthorId = cmd.AuthorId,
            AuthorName = user.DisplayName,
            Content = cmd.Content.Trim()
        };

        var updatedPost = await feedRepo.AddCommentAsync(cmd.PostId, comment);
        if (updatedPost is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        return Result<FeedPostDto>.Success(await ToDtoAsync(updatedPost, group, cmd.AuthorId, user));
    }

    public async Task<Result<FeedPostDto>> DeleteCommentAsync(Guid postId, Guid commentId, Guid userId)
    {
        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        var comment = post.Comments.FirstOrDefault(item => item.Id == commentId);
        if (comment is null)
            return Result<FeedPostDto>.Failure("Comment was not found.");

        var currentUser = await userRepo.FindByIdAsync(userId);
        if (currentUser is null)
            return Result<FeedPostDto>.Failure("User profile was not found.");

        var group = await ResolvePostGroupAsync(post);
        if (comment.AuthorId != userId && !GroupDtoMapper.CanManage(currentUser, group))
            return Result<FeedPostDto>.Failure("Permission denied.");

        var updatedPost = await feedRepo.DeleteCommentAsync(postId, commentId);
        if (updatedPost is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        return Result<FeedPostDto>.Success(await ToDtoAsync(updatedPost, group, userId, currentUser));
    }

    public async Task<Result<FeedPostDto>> ToggleReactionAsync(ToggleReactionCommand cmd)
    {
        var emoji = cmd.Emoji.Trim();
        if (!IsSupportedEmoji(emoji))
            return Result<FeedPostDto>.Failure("Choose a valid emoji.");

        var user = await userRepo.FindByIdAsync(cmd.UserId);
        if (user is null)
            return Result<FeedPostDto>.Failure("User profile was not found.");

        var post = await feedRepo.FindByIdAsync(cmd.PostId);
        if (post is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolvePostGroupAsync(post);
        if (post.Status != FeedPostStatus.Published)
            return Result<FeedPostDto>.Failure("This post is waiting for approval.");

        if (!CanParticipate(user, group))
            return Result<FeedPostDto>.Failure("Permission denied.");

        var updatedPost = await feedRepo.ToggleReactionAsync(cmd.PostId, emoji, cmd.UserId);
        if (updatedPost is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        return Result<FeedPostDto>.Success(await ToDtoAsync(updatedPost, group, cmd.UserId, user));
    }

    public async Task<Result<bool>> DeletePostAsync(Guid postId, Guid userId)
    {
        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null) return Result<bool>.Failure("Post was not found.");
        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<bool>.Failure("User profile was not found.");

        var group = await ResolvePostGroupAsync(post);
        if (post.AuthorId != userId && !GroupDtoMapper.CanManage(user, group))
            return Result<bool>.Failure("Permission denied.");

        await feedRepo.DeleteAsync(postId);
        if (attachmentStorage is not null)
            await attachmentStorage.DeleteManyAsync(post.Attachments);

        return Result<bool>.Success(true);
    }

    public async Task<Result<FeedAttachmentDownloadResult>> GetAttachmentAsync(Guid postId, Guid attachmentId, Guid userId)
    {
        if (attachmentStorage is null)
            return Result<FeedAttachmentDownloadResult>.Failure("Attachment storage is not available.");

        var user = await userRepo.FindByIdAsync(userId);
        if (user is null)
            return Result<FeedAttachmentDownloadResult>.Failure("User profile was not found.");

        var post = await feedRepo.FindByIdAsync(postId);
        if (post is null)
            return Result<FeedAttachmentDownloadResult>.Failure("Post was not found.");

        await SyncCourseGroupAssignmentsAsync();
        var group = await ResolvePostGroupAsync(post);
        if (!CanReadPost(user, post, group))
            return Result<FeedAttachmentDownloadResult>.Failure("Permission denied.");

        var attachment = post.Attachments.FirstOrDefault(item => item.Id == attachmentId);
        if (attachment is null)
            return Result<FeedAttachmentDownloadResult>.Failure("Attachment was not found.");

        var content = await attachmentStorage.OpenReadAsync(attachment);
        return content is null
            ? Result<FeedAttachmentDownloadResult>.Failure("Attachment was not found.")
            : Result<FeedAttachmentDownloadResult>.Success(new FeedAttachmentDownloadResult(attachment, content));
    }

    public async Task<Result<IReadOnlyList<FeedPostDto>>> GetPendingPostsAsync(Guid groupId, Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId);
        var group = await groupRepo.FindByIdAsync(groupId);
        if (user is null || group is null)
            return Result<IReadOnlyList<FeedPostDto>>.Failure("Group was not found.");

        if (!GroupDtoMapper.CanManage(user, group))
            return Result<IReadOnlyList<FeedPostDto>>.Failure(GroupsService.PermissionError);

        var users = await userRepo.ListAsync();
        var usersById = users.ToDictionary(account => account.Id);
        var posts = await feedRepo.GetByGroupAsync(groupId);
        return Result<IReadOnlyList<FeedPostDto>>.Success(posts
            .Where(post => post.Status == FeedPostStatus.Pending)
            .Select(post => ToDto(post, group, userId, user, usersById))
            .ToList());
    }

    public async Task<Result<FeedPostDto>> ApprovePostAsync(Guid postId, Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId);
        var post = await feedRepo.FindByIdAsync(postId);
        if (user is null || post is null)
            return Result<FeedPostDto>.Failure("Post was not found.");

        var group = await ResolvePostGroupAsync(post);
        if (!GroupDtoMapper.CanManage(user, group))
            return Result<FeedPostDto>.Failure(GroupsService.PermissionError);

        if (post.Status != FeedPostStatus.Pending)
            return Result<FeedPostDto>.Failure("This post is not waiting for approval.");

        var updatedPost = await feedRepo.SetStatusAsync(postId, FeedPostStatus.Published);
        return Result<FeedPostDto>.Success(await ToDtoAsync(updatedPost!, group, userId, user));
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

    private async Task<FeedPostDto> ToDtoAsync(FeedPost post, CampusGroup group, Guid currentUserId, User? currentUser = null)
    {
        var users = await userRepo.ListAsync();
        var usersById = users.ToDictionary(user => user.Id);
        currentUser ??= usersById.GetValueOrDefault(currentUserId);
        return ToDto(post, group, currentUserId, currentUser, usersById);
    }

    private static FeedPostDto ToDto(FeedPost post, CampusGroup group, Guid currentUserId, User? currentUser, IReadOnlyDictionary<Guid, User> usersById)
    {
        var canModerate = currentUser is not null && GroupDtoMapper.CanManage(currentUser, group);
        var comments = post.Comments
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new FeedCommentDto(
                comment.Id,
                comment.AuthorName,
                AuthorFor(comment.AuthorId, usersById),
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
            AuthorFor(post.AuthorId, usersById),
            GroupDtoMapper.ToDto(group, currentUser),
            post.Content,
            ToDto(post.Translations),
            post.Attachments.Select(attachment => ToDto(post.Id, attachment)).ToList(),
            post.CreatedAt,
            post.Status.ToString(),
            post.AllowComments,
            post.AuthorId == currentUserId || canModerate,
            post.Status == FeedPostStatus.Published && post.AllowComments && group.Settings.AllowComments && currentUser is not null && CanParticipate(currentUser, group),
            comments,
            reactions);
    }

    private static FeedPostTranslationDto? ToDto(FeedPostTranslations? translations) =>
        translations is null ? null : new FeedPostTranslationDto(translations.De, translations.En, translations.Fr);

    private static FeedAttachmentDto ToDto(Guid postId, FeedAttachment attachment) => new(
        attachment.Id,
        attachment.OriginalFileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.IsImage,
        $"/api/feed/{postId}/attachments/{attachment.Id}");

    private static ContactProfileDto? AuthorFor(Guid authorId, IReadOnlyDictionary<Guid, User> usersById) =>
        usersById.TryGetValue(authorId, out var user) ? ContactsService.ToDto(user) : null;

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

    private static bool CanParticipate(User user, CampusGroup group) => GroupDtoMapper.CanInteract(user, group);

    private static bool CanReadPost(User user, FeedPost post, CampusGroup group) =>
        post.Status == FeedPostStatus.Published
            ? GroupDtoMapper.CanReadPosts(user, group)
            : post.AuthorId == user.Id || GroupDtoMapper.CanManage(user, group);

    private static Result<FeedPostTranslations?> NormalizeTranslations(FeedPostTranslationInput? input)
    {
        if (input is null)
            return Result<FeedPostTranslations?>.Success(null);

        var de = input.De?.Trim() ?? string.Empty;
        var en = input.En?.Trim() ?? string.Empty;
        var fr = input.Fr?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(de) || string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(fr))
            return Result<FeedPostTranslations?>.Failure("Fill in all translation fields.");

        if (de.Length > 4000 || en.Length > 4000 || fr.Length > 4000)
            return Result<FeedPostTranslations?>.Failure("Post content must be at most 4000 characters long.");

        return Result<FeedPostTranslations?>.Success(new FeedPostTranslations { De = de, En = en, Fr = fr });
    }

    private static string? ValidateAttachments(IReadOnlyList<CreatePostAttachment> attachments)
    {
        if (attachments.Count > MaxAttachmentCount)
            return "A post can contain at most 5 attachments.";

        foreach (var attachment in attachments)
        {
            if (attachment.SizeBytes <= 0)
                return "Attachment files cannot be empty.";

            if (attachment.SizeBytes > MaxAttachmentBytes)
                return "Each attachment must be at most 10 MB.";

            var extension = Path.GetExtension(attachment.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedAttachmentExtensions.Contains(extension))
                return "This attachment type is not allowed.";
        }

        return null;
    }

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
        Name = "Unknown group",
        Description = "This group is no longer available.",
        Type = GroupType.Campus,
        Audience = "Archive",
        OwnerLabel = "CampusConnect",
        IconLabel = "?",
        AccentColor = "#5c6672",
        Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = false, IsDiscoverable = false }
    };
}
