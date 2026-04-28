using CampusConnect.API.DTOs.Grades;
using CampusConnect.Application.Features.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/grades")]
public class GradesController(GradesService gradesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGrades()
    {
        var userId = GetUserId();
        var summary = await gradesService.GetGradesAsync(userId);
        return Ok(summary);
    }

    [HttpPost]
    public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)
    {
        var userId = GetUserId();
        var result = await gradesService.AddGradeAsync(new AddGradeCommand(userId, request.ModuleName, request.Value, request.Ects));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/grades/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGrade(Guid id)
    {
        var userId = GetUserId();
        await gradesService.DeleteGradeAsync(id, userId);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);
}
