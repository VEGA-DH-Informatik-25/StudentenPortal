using CampusConnect.API.Common;
using CampusConnect.API.DTOs.Calendar;
using CampusConnect.Application.Features.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/calendar")]
public class CalendarController(CalendarService calendarService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetExams()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var exams = await calendarService.GetExamsAsync(userId.Value);
        return Ok(exams);
    }

    [HttpPost]
    public async Task<IActionResult> AddExam([FromBody] AddExamRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await calendarService.AddExamAsync(new AddExamCommand(
            userId.Value, request.ModuleName, request.ExamDate, request.Location, request.Notes));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/calendar/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        await calendarService.DeleteExamAsync(id, userId.Value);
        return NoContent();
    }

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);
}
