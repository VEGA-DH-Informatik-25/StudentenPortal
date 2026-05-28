using CampusConnect.API.Common;
using CampusConnect.API.DTOs.Grades;
using CampusConnect.Application.Common;
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
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var summary = await gradesService.GetGradesAsync(userId.Value);
        return Ok(summary);
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        Result<GradePlanDto> result;
        try
        {
            result = await gradesService.GetPlanAsync(userId.Value, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The DHBW study plan could not be loaded right now." });
        }

        if (!result.IsSuccess)
            return PlanFailure(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await gradesService.AddGradeAsync(new AddGradeCommand(userId.Value, request.ModuleName, request.Value, request.Ects, request.ModuleCode), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/grades/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGrade(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        await gradesService.DeleteGradeAsync(id, userId.Value);
        return NoContent();
    }

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);

    private IActionResult PlanFailure(string? error) => error switch
    {
        "User profile was not found." => BadRequest(new { error }),
        "No valid course is assigned to your profile." => BadRequest(new { error }),
        "No DHBW study plan was found for your course." => NotFound(new { error }),
        _ => BadRequest(new { error = error ?? "The study plan could not be loaded." })
    };
}
