using System.Security.Claims;
using CampusConnect.API.DTOs.Admin;
using CampusConnect.API.DTOs.Courses;
using CampusConnect.Application.Features.Admin;
using CampusConnect.Application.Features.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController(AdminUsersService adminUsersService, CoursesService coursesService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await adminUsersService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPatch("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.UpdateRoleAsync(new UpdateUserRoleCommand(id, request.Role, GetUserId()), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPatch("users/{id:guid}/course")]
    public async Task<IActionResult> UpdateUserCourse(Guid id, [FromBody] UpdateUserCourseRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.UpdateCourseAsync(new UpdateUserCourseCommand(id, request.CourseCode), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.DeleteUserAsync(id, GetUserId(), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
    {
        var courses = await coursesService.GetCoursesAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var result = await coursesService.CreateCourseAsync(new CreateCourseCommand(request.Code, request.StudyProgram, request.Semester), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/admin/courses/{result.Value!.Code}", result.Value);
    }

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);
}