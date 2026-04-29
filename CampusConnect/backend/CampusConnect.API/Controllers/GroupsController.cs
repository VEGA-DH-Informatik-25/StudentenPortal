using CampusConnect.API.Common;
using CampusConnect.API.DTOs.Groups;
using CampusConnect.Application.Features.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupsController(GroupsService groupsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var groups = await groupsService.GetGroupsForUserAsync(userId.Value);
        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await groupsService.CreateGroupAsync(new CreateGroupCommand(
            userId.Value,
            request.Name,
            request.Description,
            request.Audience,
            request.AllowStudentPosts,
            request.AllowComments,
            request.RequiresApproval,
            request.IsDiscoverable));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/groups/{result.Value!.Id}", result.Value);
    }

    [HttpGet("{id:guid}/settings")]
    public async Task<IActionResult> GetSettings(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await groupsService.GetSettingsDetailsAsync(id, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] UpdateGroupSettingsRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await groupsService.UpdateSettingsAsync(
            id,
            userId.Value,
            new UpdateGroupSettingsCommand(request.AllowStudentPosts, request.AllowComments, request.RequiresApproval, request.IsDiscoverable));

        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/assignments")]
    public async Task<IActionResult> UpdateAssignments(Guid id, [FromBody] UpdateGroupAssignmentsRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await groupsService.UpdateAssignmentsAsync(id, userId.Value, new UpdateGroupAssignmentsCommand(request.UserIds));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/member-permissions")]
    public async Task<IActionResult> UpdateMemberPermissions(Guid id, [FromBody] UpdateGroupMemberPermissionsRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var permissions = request.Permissions
            .Select(item => new UpdateGroupMemberPermissionCommand(item.UserId, item.Permission))
            .ToList();
        var result = await groupsService.UpdateMemberPermissionsAsync(id, userId.Value, new UpdateGroupMemberPermissionsCommand(permissions));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> JoinGroup(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var result = await groupsService.JoinGroupAsync(id, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    private IActionResult ToFailureResult(string? error) =>
        error == GroupsService.PermissionError
            ? Forbid()
            : BadRequest(new { error });

    private Guid? GetCurrentUserId() => CurrentUser.GetUserId(User);
}
