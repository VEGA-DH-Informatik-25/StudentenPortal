using CampusConnect.API.DTOs.Feed;
using CampusConnect.Application.Features.Feed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/feed")]
public class FeedController(FeedService feedService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var posts = await feedService.GetFeedAsync(GetUserId(), page, pageSize);
        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var result = await feedService.CreatePostAsync(new CreatePostCommand(GetUserId(), request.GroupId, request.Content));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/feed/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var result = await feedService.DeletePostAsync(id, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        var result = await feedService.AddCommentAsync(new CreateCommentCommand(id, GetUserId(), request.Content));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId)
    {
        var result = await feedService.DeleteCommentAsync(postId, commentId, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reactions")]
    public async Task<IActionResult> ToggleReaction(Guid id, [FromBody] ToggleReactionRequest request)
    {
        var result = await feedService.ToggleReactionAsync(new ToggleReactionCommand(id, GetUserId(), request.Emoji));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")!.Value);
}
