using CampusConnect.Domain.Entities;
using CampusConnect.Infrastructure.Repositories;

namespace CampusConnect.API.Tests;

public sealed class InMemoryFeedRepositoryTests
{
    [Fact]
    public async Task FindByIdAsync_ShouldReturnCloneInsteadOfStoredPostReference()
    {
        var repository = new InMemoryFeedRepository();
        var post = new FeedPost
        {
            AuthorId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            AuthorName = "Alice",
            Content = "Original"
        };

        await repository.AddAsync(post);
        var firstRead = await repository.FindByIdAsync(post.Id);
        Assert.NotNull(firstRead);

        firstRead!.Content = "Mutated outside repository";
        firstRead.Comments.Add(new FeedComment { AuthorId = Guid.NewGuid(), AuthorName = "Bob", Content = "Leaked" });

        var secondRead = await repository.FindByIdAsync(post.Id);

        Assert.NotNull(secondRead);
        Assert.Equal("Original", secondRead!.Content);
        Assert.Empty(secondRead.Comments);
    }

    [Fact]
    public async Task ToggleReactionAsync_ShouldReturnUpdatedCloneWithoutLeakingStoredReactionSet()
    {
        var repository = new InMemoryFeedRepository();
        var post = new FeedPost
        {
            AuthorId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            AuthorName = "Alice",
            Content = "Original"
        };
        var userId = Guid.NewGuid();

        await repository.AddAsync(post);
        var updatedPost = await repository.ToggleReactionAsync(post.Id, "👍", userId);
        Assert.NotNull(updatedPost);

        updatedPost!.Reactions.Single().UserIds.Clear();

        var storedPost = await repository.FindByIdAsync(post.Id);
        Assert.NotNull(storedPost);
        Assert.Contains(userId, storedPost!.Reactions.Single().UserIds);
    }
}
