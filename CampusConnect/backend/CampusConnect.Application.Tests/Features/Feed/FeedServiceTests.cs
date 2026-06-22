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
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var users = new FakeUserRepository(user);
        var groups = new FakeGroupRepository(group);
        var feed = new FakeFeedRepository();
        var service = new FeedService(feed, groups, users);

        var result = await service.CreatePostAsync(new CreatePostCommand(user.Id, group.Id, "Exam preparation at 4 p.m."));

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id, result.Value!.Group.Id);
        Assert.Equal("Course TIF25A", result.Value.Group.Name);
        Assert.Equal("alice@dhbw-loerrach.de", result.Value.Author?.Email);
        Assert.Equal("TIF25A", result.Value.Author?.Course);
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
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = new CampusGroup
        {
            Name = "Official announcements",
            Type = GroupType.Official,
            Audience = "All students",
            OwnerLabel = "University",
            IconLabel = "OF",
            AssignedUserIds = [user.Id],
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        };
        var service = new FeedService(new FakeFeedRepository(), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.CreatePostAsync(new CreatePostCommand(user.Id, group.Id, "Please publish this"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Members are not allowed to publish posts in this group.", result.Error);
    }

    [Fact]
    public async Task CreatePostAsync_RejectsPostsInUnassignedPublicGroup()
    {
        var user = new User
        {
            DisplayName = "Clara",
            Email = "clara@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "David",
            Email = "david@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        var service = new FeedService(new FakeFeedRepository(), new FakeGroupRepository(group), new FakeUserRepository(user, owner));

        var result = await service.CreatePostAsync(new CreatePostCommand(user.Id, group.Id, "Am I included?"));

        Assert.False(result.IsSuccess);
        Assert.Equal("You can only post in groups assigned to you.", result.Error);
    }

    [Fact]
    public async Task CreatePostAsync_RejectsMembersWhenStudentPostsDisabled()
    {
        var owner = new User
        {
            DisplayName = "David",
            Email = "david@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Clara",
            Email = "clara@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        group.AssignedUserIds.Add(member.Id);
        group.Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = true, RequiresApproval = false, IsDiscoverable = true };
        var service = new FeedService(new FakeFeedRepository(), new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.CreatePostAsync(new CreatePostCommand(member.Id, group.Id, "Can I post?"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Members are not allowed to publish posts in this group.", result.Error);
    }

    [Fact]
    public async Task CreatePostAsync_WhenApprovalIsRequired_LeavesMemberPostPending()
    {
        var owner = Student("Owner", "owner@dhbw-loerrach.de");
        var member = Student("Member", "member@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        group.AssignedUserIds.Add(member.Id);
        group.Settings.RequiresApproval = true;
        var feed = new FakeFeedRepository();
        var service = new FeedService(feed, new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.CreatePostAsync(new CreatePostCommand(member.Id, group.Id, "Please review", AllowComments: false));

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.False(result.Value.AllowComments);
        Assert.Equal(FeedPostStatus.Pending, feed.Posts.Single().Status);
    }

    [Fact]
    public async Task CreatePostAsync_WhenApprovalIsRequired_PublishesModeratorPostImmediately()
    {
        var owner = Student("Owner", "owner@dhbw-loerrach.de");
        var moderator = Student("Moderator", "moderator@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        group.AssignedUserIds.Add(moderator.Id);
        group.MemberRoles[moderator.Id] = GroupRole.Moderator;
        group.Settings.RequiresApproval = true;
        var service = new FeedService(new FakeFeedRepository(), new FakeGroupRepository(group), new FakeUserRepository(owner, moderator));

        var result = await service.CreatePostAsync(new CreatePostCommand(moderator.Id, group.Id, "Published now"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Published", result.Value!.Status);
    }

    [Fact]
    public async Task GetFeedAsync_HidesPendingPosts()
    {
        var owner = Student("Owner", "owner@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        var pending = new FeedPost
        {
            AuthorId = owner.Id,
            AuthorName = owner.DisplayName,
            GroupId = group.Id,
            Content = "Waiting",
            Status = FeedPostStatus.Pending
        };
        var service = new FeedService(new FakeFeedRepository(pending), new FakeGroupRepository(group), new FakeUserRepository(owner));

        var result = await service.GetFeedAsync(owner.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ApprovePostAsync_AllowsModeratorAndPublishesPost()
    {
        var owner = Student("Owner", "owner@dhbw-loerrach.de");
        var moderator = Student("Moderator", "moderator@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        group.AssignedUserIds.Add(moderator.Id);
        group.MemberRoles[moderator.Id] = GroupRole.Moderator;
        var post = new FeedPost
        {
            AuthorId = owner.Id,
            AuthorName = owner.DisplayName,
            GroupId = group.Id,
            Content = "Waiting",
            Status = FeedPostStatus.Pending
        };
        var feed = new FakeFeedRepository(post);
        var service = new FeedService(feed, new FakeGroupRepository(group), new FakeUserRepository(owner, moderator));

        var result = await service.ApprovePostAsync(post.Id, moderator.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Published", result.Value!.Status);
        Assert.Equal(FeedPostStatus.Published, (await feed.FindByIdAsync(post.Id))!.Status);
    }

    [Fact]
    public async Task GetFeedAsync_HidesPrivateUnassignedGroupPosts()
    {
        var user = new User
        {
            DisplayName = "Elif",
            Email = "elif@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Farid",
            Email = "farid@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: false);
        var post = new FeedPost { AuthorId = owner.Id, AuthorName = owner.DisplayName, GroupId = group.Id, Content = "Private meeting point" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user, owner));

        var result = await service.GetFeedAsync(user.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeedAsync_HidesPublicUnassignedGroupPosts()
    {
        var user = new User
        {
            DisplayName = "Elif",
            Email = "elif@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var owner = new User
        {
            DisplayName = "Farid",
            Email = "farid@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        var post = new FeedPost { AuthorId = owner.Id, AuthorName = owner.DisplayName, GroupId = group.Id, Content = "Publicly discoverable, internally readable" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user, owner));

        var result = await service.GetFeedAsync(user.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddCommentAsync_WhenGroupAllowsComments_AppendsCommentToPost()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Study group?" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.AddCommentAsync(new CreateCommentCommand(post.Id, user.Id, "I am in."));

        Assert.True(result.IsSuccess);
        var comment = Assert.Single(result.Value!.Comments);
        Assert.Equal("I am in.", comment.Content);
        Assert.Equal("alice@dhbw-loerrach.de", comment.Author?.Email);
        Assert.True(comment.CanDelete);
    }

    [Fact]
    public async Task AddCommentAsync_WhenPostDisablesComments_RejectsComment()
    {
        var user = Student("Alice", "alice@dhbw-loerrach.de");
        var group = CourseGroup("TIF25A");
        group.AssignedUserIds.Add(user.Id);
        var post = new FeedPost
        {
            AuthorId = user.Id,
            AuthorName = user.DisplayName,
            GroupId = group.Id,
            Content = "Read only",
            AllowComments = false
        };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.AddCommentAsync(new CreateCommentCommand(post.Id, user.Id, "No comment"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Comments are closed in this group.", result.Error);
    }

    [Fact]
    public async Task DeletePostAsync_AllowsGroupModeratorToRemoveForeignPost()
    {
        var owner = Student("Owner", "owner@dhbw-loerrach.de");
        var moderator = Student("Moderator", "moderator@dhbw-loerrach.de");
        var member = Student("Member", "member@dhbw-loerrach.de");
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        group.AssignedUserIds.UnionWith([moderator.Id, member.Id]);
        group.MemberRoles[moderator.Id] = GroupRole.Moderator;
        var post = new FeedPost { AuthorId = member.Id, AuthorName = member.DisplayName, GroupId = group.Id, Content = "Remove me" };
        var feed = new FakeFeedRepository(post);
        var service = new FeedService(feed, new FakeGroupRepository(group), new FakeUserRepository(owner, moderator, member));

        var result = await service.DeletePostAsync(post.Id, moderator.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(feed.Posts);
    }

    [Fact]
    public async Task AddCommentAsync_RejectsNonMembers()
    {
        var owner = new User
        {
            DisplayName = "David",
            Email = "david@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Clara",
            Email = "clara@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        var post = new FeedPost { AuthorId = owner.Id, AuthorName = owner.DisplayName, GroupId = group.Id, Content = "Room available" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.AddCommentAsync(new CreateCommentCommand(post.Id, member.Id, "Thanks!"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Permission denied.", result.Error);
    }

    [Fact]
    public async Task ToggleReactionAsync_TogglesCurrentUserReaction()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Study group?" };
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

    [Fact]
    public async Task ToggleReactionAsync_RejectsNonMembers()
    {
        var owner = new User
        {
            DisplayName = "David",
            Email = "david@dhbw-loerrach.de",
            StudyProgram = "BWL",
            Course = "BWL25A",
            Role = UserRole.Student
        };
        var member = new User
        {
            DisplayName = "Clara",
            Email = "clara@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var group = SocialGroup(owner.Id, isDiscoverable: true);
        var post = new FeedPost { AuthorId = owner.Id, AuthorName = owner.DisplayName, GroupId = group.Id, Content = "Room available" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(owner, member));

        var result = await service.ToggleReactionAsync(new ToggleReactionCommand(post.Id, member.Id, "👍"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Permission denied.", result.Error);
    }

    [Fact]
    public async Task ToggleReactionAsync_AcceptsCustomEmoji()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Project idea" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.ToggleReactionAsync(new ToggleReactionCommand(post.Id, user.Id, "🚀"));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Reactions, reaction => reaction.Emoji == "🚀" && reaction.ReactedByCurrentUser);
    }

    [Fact]
    public async Task ToggleReactionAsync_RejectsPlainTextReaction()
    {
        var user = new User
        {
            DisplayName = "Alice",
            Email = "alice@dhbw-loerrach.de",
            StudyProgram = "Computer Science",
            Course = "TIF25A"
        };
        var group = CourseGroup("TIF25A");
        var post = new FeedPost { AuthorId = user.Id, AuthorName = user.DisplayName, GroupId = group.Id, Content = "Project idea" };
        var service = new FeedService(new FakeFeedRepository(post), new FakeGroupRepository(group), new FakeUserRepository(user));

        var result = await service.ToggleReactionAsync(new ToggleReactionCommand(post.Id, user.Id, "nice"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Choose a valid emoji.", result.Error);
    }

    private static CampusGroup CourseGroup(string courseCode) => new()
    {
        Name = $"Course {courseCode}",
        Type = GroupType.Course,
        Audience = courseCode,
        CourseCode = courseCode,
        OwnerLabel = "Computer Science",
        IconLabel = "TI",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = false }
    };

    private static User Student(string displayName, string email) => new()
    {
        DisplayName = displayName,
        Email = email,
        StudyProgram = "Computer Science",
        Course = "TIF25A",
        Role = UserRole.Student
    };

    private static CampusGroup SocialGroup(Guid ownerId, bool isDiscoverable) => new()
    {
        Name = "Housing in Loerrach",
        Description = "Exchange about rooms and commuting",
        Type = GroupType.Campus,
        Audience = "Students",
        OwnerUserId = ownerId,
        OwnerLabel = "Community",
        IconLabel = "WG",
        Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = isDiscoverable },
        AssignedUserIds = [ownerId]
    };

    private sealed class FakeFeedRepository(params FeedPost[] posts) : IFeedRepository
    {
        private readonly List<FeedPost> _posts = posts.ToList();

        public IReadOnlyList<FeedPost> Posts => _posts;

        public Task<IReadOnlyList<FeedPost>> GetAllAsync(int page, int pageSize) => Task.FromResult<IReadOnlyList<FeedPost>>(_posts);

        public Task<FeedPost?> FindByIdAsync(Guid id) => Task.FromResult(_posts.FirstOrDefault(post => post.Id == id));

        public Task<IReadOnlyList<FeedPost>> GetByGroupAsync(Guid groupId) =>
            Task.FromResult<IReadOnlyList<FeedPost>>(_posts.Where(post => post.GroupId == groupId).ToList());

        public Task AddAsync(FeedPost post)
        {
            _posts.Add(post);
            return Task.CompletedTask;
        }

        public Task<FeedPost?> SetStatusAsync(Guid id, FeedPostStatus status)
        {
            var post = _posts.FirstOrDefault(post => post.Id == id);
            if (post is not null)
                post.Status = status;
            return Task.FromResult(post);
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

        public Task DeleteByGroupAsync(Guid groupId)
        {
            _posts.RemoveAll(post => post.GroupId == groupId);
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

        public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds)
        {
            var group = _groups.First(group => group.Id == id);
            var assigned = group.AssignedUserIds.ToHashSet();
            foreach (var userId in userIds)
                assigned.Add(userId);
            group.AssignedUserIds = assigned;
            return Task.CompletedTask;
        }

        public Task RemoveMemberAsync(Guid id, Guid userId)
        {
            var group = _groups.First(group => group.Id == id);
            var assigned = group.AssignedUserIds.ToHashSet();
            assigned.Remove(userId);
            group.AssignedUserIds = assigned;
            group.MemberRoles.Remove(userId);
            return Task.CompletedTask;
        }

        public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role)
        {
            var group = _groups.First(group => group.Id == id);
            if (role == GroupRole.Moderator)
                group.MemberRoles[userId] = GroupRole.Moderator;
            else
                group.MemberRoles.Remove(userId);
            return Task.CompletedTask;
        }

        public Task AddJoinRequestAsync(Guid id, Guid userId)
        {
            _groups.First(group => group.Id == id).PendingJoinRequests.Add(userId);
            return Task.CompletedTask;
        }

        public Task RemoveJoinRequestAsync(Guid id, Guid userId)
        {
            _groups.First(group => group.Id == id).PendingJoinRequests.Remove(userId);
            return Task.CompletedTask;
        }

        public Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds)
        {
            var group = _groups.First(group => group.Id == id);
            foreach (var userId in userIds)
                group.Invitations.Add(userId);
            return Task.CompletedTask;
        }

        public Task RemoveInvitationAsync(Guid id, Guid userId)
        {
            _groups.First(group => group.Id == id).Invitations.Remove(userId);
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
