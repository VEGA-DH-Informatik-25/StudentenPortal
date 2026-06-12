using CampusConnect.API.Common;
using CampusConnect.API.DTOs.Feed;
using CampusConnect.Application.Features.Feed;
using CampusConnect.Application.Features.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/feed")]
public class FeedController(FeedService feedService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var posts = await feedService.GetFeedAsync(userId.Value, page, pageSize);
        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.CreatePostAsync(new CreatePostCommand(userId.Value, request.GroupId, request.Content, request.AllowComments));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/feed/{result.Value!.Id}", result.Value);
    }

    [HttpGet("/api/groups/{groupId:guid}/pending-posts")]
    public async Task<IActionResult> GetPendingPosts(Guid groupId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.GetPendingPostsAsync(groupId, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApprovePost(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.ApprovePostAsync(id, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.DeletePostAsync(id, userId.Value);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.AddCommentAsync(new CreateCommentCommand(id, userId.Value, request.Content));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.DeleteCommentAsync(postId, commentId, userId.Value);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reactions")]
    public async Task<IActionResult> ToggleReaction(Guid id, [FromBody] ToggleReactionRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await feedService.ToggleReactionAsync(new ToggleReactionCommand(id, userId.Value, request.Emoji));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    private IActionResult ToFailureResult(string? error) =>
        error == GroupsService.PermissionError
            ? Forbid()
            : BadRequest(new { error });

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);
}
