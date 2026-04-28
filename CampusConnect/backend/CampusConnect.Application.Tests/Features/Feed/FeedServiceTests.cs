using CampusConnect.Application.Features.Feed;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Feed;

public class FeedServiceTests
{
    [Fact]
    public async Task CreatePostAsync_AddsSelectedGroupMetadataToPost()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 3,
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var users = new FakeUserRepository(user);
        var groups = new FakeGroupRepository(group);
        var feed = new FakeFeedRepository();
        var service = new FeedService(feed, groups, users);

        var result = await service.CreatePostAsync(new CreatePostCommand(user.Id, group.Id, "Klausurvorbereitung um 16 Uhr"));

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id, result.Value!.Group.Id);
        Assert.Equal("Kurs TIF25A", result.Value.Group.Name);
        Assert.True(result.Value.CanDelete);
        Assert.Equal(group.Id, feed.Posts.Single().GroupId);
    }

    [Fact]
    public async Task CreatePostAsync_RejectsStudentPostsInLockedGroup()
    {
        var user = new User
        {
            DisplayName = "Ben",
            Email = "ben@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 2,
            Course = "TIF25A"
        };
        var group = new CampusGroup
        {
            Name = "Offizielle Mitteilungen",
            Type = GroupType.Official,
            Audience = "Alle Studierenden",
            OwnerLabel = "Hochschule",
            IconLabel = "OF",
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        };
        var service = new FeedService(new FakeFeedRepository(), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.CreatePostAsync(new CreatePostCommand(user.Id, group.Id, "Bitte veröffentlichen"));

        Assert.False(result.IsSuccess);
        Assert.Equal("In dieser Gruppe dürfen Studierende keine Beiträge veröffentlichen.", result.Error);
    }

    [Fact]
    public async Task AddCommentAsync_WhenGroupAllowsComments_AppendsCommentToPost()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 3,
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Lerngruppe?" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.AddCommentAsync(new CreateCommentCommand(post.Id, user.Id, "Ich bin dabei."));

        Assert.True(result.IsSuccess);
        var comment = Assert.Single(result.Value!.Comments);
        Assert.Equal("Ich bin dabei.", comment.Content);
        Assert.True(comment.CanDelete);
    }

    [Fact]
    public async Task ToggleReactionAsync_TogglesCurrentUserReaction()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Informatik",
            Semester = 3,
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Lerngruppe?" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var added = await service.ToggleReactionAsync(new ToggleReactionCommand(post.Id, user.Id, "👍"));
        var removed = await service.ToggleReactionAsync(new ToggleReactionCommand(post.Id, user.Id, "👍"));

        Assert.True(added.IsSuccess);
        var reaction = Assert.Single(added.Value!.Reactions);
        Assert.Equal(1, reaction.Count);
        Assert.True(reaction.ReactedByCurrentUser);
        Assert.True(removed.IsSuccess);
        Assert.Empty(removed.Value!.Reactions);
    }

    private static CampusGroup CourseGroup(string courseCode) => new()
    {
        Name = $"Kurs {courseCode}",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = "Informatik",
        IconLabel = "TI",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
    };

    private sealed class FakeFeedRepository(params FeedPost[] posts) : IFeedRepository
    {
        private readonly List<FeedPost> _posts = posts.ToList();

        public IReadOnlyList<FeedPost> Posts => _posts;

        public Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize) => Task.FromResult<IReadOnlyList<FeedPost>>(_posts);

        public Task<FeedPost?> FindByIdAsync(Guid id) => Task.FromResult(_posts.FirstOrDefault(post => post.Id == id));

        public Task AddAsync(FeedPost post)
        {
            _posts.Add(post);
            return Task.CompletedTask;
        }

        public Task<FeedPost?> AddCommentAsync(Guid postId, FeedComment comment)
        {
            var post = _posts.FirstOrDefault(post => post.Id == postId);
            post?.Comments.Add(comment);
            return Task.FromResult(post);
        }

        public Task<FeedPost?> DeleteCommentAsync(Guid postId, Guid commentId)
        {
            var post = _posts.FirstOrDefault(post => post.Id == postId);
            post?.Comments.RemoveAll(comment => comment.Id == commentId);
            return Task.FromResult(post);
        }

        public Task<FeedPost?> ToggleReactionAsync(Guid postId, string emoji, Guid userId)
        {
            var post = _posts.FirstOrDefault(post => post.Id == postId);
            if (post is null)
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

            return Task.FromResult<FeedPost?>(post);
        }

        public Task DeleteAsync(Guid id)
        {
            _posts.RemoveAll(post => post.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroupRepository(params CampusGroup[] groups) : IGroupRepository
    {
        private readonly List<CampusGroup> _groups = groups.ToList();

        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>(_groups);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult(_groups.FirstOrDefault(group => group.Id == id));

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
        {
            var existing = _groups.FirstOrDefault(group => group.CourseCode == courseCode);
            if (existing is not null)
                return Task.FromResult(existing);

            var group = CourseGroup(courseCode);
            _groups.Add(group);
            return Task.FromResult(group);
        }

        public Task AddAsync(CampusGroup group)
        {
            _groups.Add(group);
            return Task.CompletedTask;
        }

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings)
        {
            var group = _groups.First(group => group.Id == id);
            group.Settings = settings;
            return Task.CompletedTask;
        }

        public Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds)
        {
            var group = _groups.First(group => group.Id == id);
            group.AssignedUserIds = assignedUserIds.ToHashSet();
            return Task.CompletedTask;
        }

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds)
        {
            var group = _groups.FirstOrDefault(group => string.Equals(group.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase));
            if (group is not null)
                group.AssignedUserIds = assignedUserIds.ToHashSet();

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly List<User> _users = users.ToList();

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(_users);

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Email == email));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.RemoveAll(user => user.Id == id);
            return Task.CompletedTask;
        }
    }
}
