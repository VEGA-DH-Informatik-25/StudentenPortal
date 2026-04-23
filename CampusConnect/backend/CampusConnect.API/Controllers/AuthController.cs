using CampusConnect.API.DTOs.Auth;
using CampusConnect.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[ApiController]
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

        return Ok(new AuthResponse(result.Value!.Token, result.Value.DisplayName, result.Value.Email, result.Value.Role));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(new LoginCommand(request.Email, request.Password));

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(new AuthResponse(result.Value!.Token, result.Value.DisplayName, result.Value.Email, result.Value.Role));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")!.Value);
        var user = await authService.GetByIdAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { user.Email, user.DisplayName, user.StudyProgram, user.Semester, user.Course, Role = user.Role.ToString() });
    }
}
