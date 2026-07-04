using CampusConnect.API.DTOs.Auth;
using CampusConnect.API.Common;
using CampusConnect.Application.Common;
using CampusConnect.Application.Features.Auth;
using Microsoft.AspNetCore.Authentication;
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
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        if (request is null)
            return Unauthorized(new { error = "Invalid email address or password." });

        var result = await authService.LoginAsync(new LoginCommand(
            request.Email,
            request.Password,
            GetClientIpAddress(),
            GetDeviceIdentifier()));

        if (!result.IsSuccess)
        {
            if (result.Error == AuthService.LoginRateLimitExceededError)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.Error });

            return Unauthorized(new { error = result.Error });
        }

        await SignInBrowserSessionAsync(result.Value!);
        return Ok(ToAuthResponse(result.Value!));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Browser);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await authService.GetProfileAsync(userId.Value);
        if (!result.IsSuccess)
            return ToProfileError(result);

        await SignInBrowserSessionAsync(result.Value!);
        return Ok(ToUserProfileResponse(result.Value!));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await authService.UpdateProfileAsync(userId.Value, new UpdateUserProfileCommand(
            request.DisplayName,
            request.Course,
            request.PhoneNumber,
            request.Location));

        if (!result.IsSuccess)
            return ToProfileError(result);

        await SignInBrowserSessionAsync(result.Value!);
        return Ok(ToUserProfileResponse(result.Value!));
    }

    [HttpPost("change-initial-password")]
    public async Task<IActionResult> ChangeInitialPassword([FromBody] ChangeInitialPasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await authService.ChangeInitialPasswordAsync(userId.Value, new ChangeInitialPasswordCommand(request.CurrentPassword, request.NewPassword));
        if (!result.IsSuccess)
            return ToProfileError(result);

        await SignInBrowserSessionAsync(result.Value!);
        return Ok(ToUserProfileResponse(result.Value!));
    }

    [HttpPost("onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await authService.CompleteOnboardingAsync(userId.Value);
        if (!result.IsSuccess)
            return ToProfileError(result);

        await SignInBrowserSessionAsync(result.Value!);
        return Ok(ToUserProfileResponse(result.Value!));
    }

    private Guid? GetCurrentUserId()
        => CurrentUser.GetUserId(User);

    private string GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    private string GetDeviceIdentifier() =>
        Request.Headers.UserAgent.ToString();

    private Task SignInBrowserSessionAsync(AuthResult result) =>
        SignInBrowserSessionAsync(result.Profile);

    private Task SignInBrowserSessionAsync(UserProfileResult profile) =>
        HttpContext.SignInAsync(
            AuthSchemes.Browser,
            ToClaimsPrincipal(profile),
            new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(AuthSchemes.IdleTimeout)
            });

    private static ClaimsPrincipal ToClaimsPrincipal(UserProfileResult profile)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()),
            new Claim(ClaimTypes.Email, profile.Email),
            new Claim(ClaimTypes.Name, profile.DisplayName),
            new Claim(ClaimTypes.Role, profile.Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSchemes.Browser));
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
        new(profile.Id, profile.Email, profile.DisplayName, profile.StudyProgram, profile.Course, profile.PhoneNumber, profile.Location, profile.Role, profile.MustChangePassword, profile.OnboardingCompleted, profile.OnboardingCompletedAt, profile.CreatedAt);
}
