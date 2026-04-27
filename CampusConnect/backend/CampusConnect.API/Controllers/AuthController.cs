using CampusConnect.API.DTOs.Auth;
using CampusConnect.Application.Common;
using CampusConnect.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(new RegisterCommand(
            request.Email, request.Password, request.DisplayName,
            request.StudyProgram, request.Semester, request.Course));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(ToAuthResponse(result.Value!));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(new LoginCommand(request.Email, request.Password));

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(ToAuthResponse(result.Value!));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await authService.GetProfileAsync(userId.Value);
        if (!result.IsSuccess)
            return ToProfileError(result);

        return Ok(ToUserProfileResponse(result.Value!));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await authService.UpdateProfileAsync(userId.Value, new UpdateUserProfileCommand(
            request.DisplayName, request.StudyProgram, request.Semester, request.Course));

        if (!result.IsSuccess)
            return ToProfileError(result);

        return Ok(ToUserProfileResponse(result.Value!));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private IActionResult ToProfileError(Result<UserProfileResult> result) =>
        result.Error == AuthService.UserProfileNotFoundError
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });

    private static AuthResponse ToAuthResponse(AuthResult result)
    {
        var profile = ToUserProfileResponse(result.Profile);
        return new AuthResponse(result.Token, profile.DisplayName, profile.Email, profile.Role, profile);
    }

    private static UserProfileResponse ToUserProfileResponse(UserProfileResult profile) =>
        new(profile.Id, profile.Email, profile.DisplayName, profile.StudyProgram, profile.Semester, profile.Course, profile.Role, profile.CreatedAt);
}
