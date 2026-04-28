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
        var posts = await feedService.GetFeedAsync(page, pageSize);
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

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")!.Value);
}
