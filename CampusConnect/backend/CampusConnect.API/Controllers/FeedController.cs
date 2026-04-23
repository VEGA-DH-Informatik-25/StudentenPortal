using CampusConnect.API.DTOs.Feed;
using CampusConnect.Application.Features.Feed;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

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
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")!.Value);
        var authorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unbekannt";

        var result = await feedService.CreatePostAsync(new CreatePostCommand(userId, authorName, request.Content));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/feed/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")!.Value);
        var result = await feedService.DeletePostAsync(id, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}
