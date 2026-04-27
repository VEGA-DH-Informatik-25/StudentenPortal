using CampusConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/timetable")]
public class TimetableController(ITimetableService timetableService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTimetable([FromQuery] string course, [FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(course))
            return BadRequest(new { error = "Bitte einen Kurs auswählen." });

        try
        {
            var timetable = await timetableService.GetTimetableAsync(course, days, cancellationToken);
            return Ok(timetable);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}