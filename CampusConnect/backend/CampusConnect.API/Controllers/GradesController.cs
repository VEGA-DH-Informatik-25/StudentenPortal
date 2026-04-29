using CampusConnect.API.Common;
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
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var summary = await gradesService.GetGradesAsync(userId.Value);
        return Ok(summary);
    }

    [HttpPost]
    public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await gradesService.AddGradeAsync(new AddGradeCommand(userId.Value, request.ModuleName, request.Value, request.Ects));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/grades/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGrade(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        await gradesService.DeleteGradeAsync(id, userId.Value);
        return NoContent();
    }

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);
}
