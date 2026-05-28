using CampusConnect.API.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/timetable")]
public class TimetableController(ITimetableService timetableService, AuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTimetable([FromQuery] string? course = null, [FromQuery] int days = 30, [FromQuery] DateOnly? from = null, CancellationToken cancellationToken = default)
    {
        var resolvedCourse = string.IsNullOrWhiteSpace(course)
            ? await ResolveCurrentUserCourseAsync()
            : course;

        if (string.IsNullOrWhiteSpace(resolvedCourse))
            return BadRequest(new { error = "Choose a course." });

        try
        {
            var timetable = await timetableService.GetTimetableAsync(resolvedCourse, days, from, cancellationToken);
            return Ok(timetable);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<string?> ResolveCurrentUserCourseAsync()
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return null;

        var profile = await authService.GetProfileAsync(userId.Value);
        return profile.IsSuccess ? profile.Value!.Course : null;
    }
}