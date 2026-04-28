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
        var userId = GetUserId();
        var exams = await calendarService.GetExamsAsync(userId);
        return Ok(exams);
    }

    [HttpPost]
    public async Task<IActionResult> AddExam([FromBody] AddExamRequest request)
    {
        var userId = GetUserId();
        var result = await calendarService.AddExamAsync(new AddExamCommand(
            userId, request.ModuleName, request.ExamDate, request.Location, request.Notes));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/calendar/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        var userId = GetUserId();
        await calendarService.DeleteExamAsync(id, userId);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);
}
