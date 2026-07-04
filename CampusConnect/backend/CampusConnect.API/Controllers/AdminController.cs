using CampusConnect.API.Common;
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

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.CreateUserAsync(
            new CreateAdminUserCommand(request.FirstName, request.LastName, request.Email, request.Role, request.CourseCode, request.InitialPassword, request.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/admin/users/{result.Value!.Id}", result.Value);
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await adminUsersService.UpdateUserAsync(
            new UpdateAdminUserCommand(id, request.DisplayName, request.Email, request.Role, request.CourseCode, request.IsActive, currentUserId.Value),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await adminUsersService.UpdateStatusAsync(new UpdateUserStatusCommand(id, request.IsActive, currentUserId.Value), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPatch("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await adminUsersService.UpdateRoleAsync(new UpdateUserRoleCommand(id, request.Role, currentUserId.Value), cancellationToken);
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

    [HttpPatch("users/{id:guid}/password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.ResetPasswordAsync(new ResetUserPasswordCommand(id, request.InitialPassword), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await adminUsersService.DeleteUserAsync(id, currentUserId.Value, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
    {
        var courses = await coursesService.GetCoursesAsync(includeSystemCourses: true, cancellationToken);
        return Ok(courses);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var result = await coursesService.CreateCourseAsync(new CreateCourseCommand(request.Code, request.StudyProgram), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/admin/courses/{result.Value!.Code}", result.Value);
    }

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);
}
