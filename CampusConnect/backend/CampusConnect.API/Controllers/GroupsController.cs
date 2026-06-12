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
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var groups = await groupsService.GetGroupsForUserAsync(userId.Value);
        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.CreateGroupAsync(new CreateGroupCommand(
            userId.Value,
            request.Name,
            request.Description,
            request.Audience,
            request.AllowStudentPosts,
            request.AllowComments,
            request.RequiresApproval,
            request.IsDiscoverable,
            request.Type,
            request.CourseCode,
            request.JoinRule,
            request.OfficialCategory));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Created($"/api/groups/{result.Value!.Id}", result.Value);
    }

    [HttpGet("{id:guid}/settings")]
    public async Task<IActionResult> GetSettings(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

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
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.UpdateSettingsAsync(
            id,
            userId.Value,
            new UpdateGroupSettingsCommand(request.AllowStudentPosts, request.AllowComments, request.RequiresApproval, request.IsDiscoverable, request.JoinRule));

        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.DeleteGroupAsync(id, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return NoContent();
    }

    [HttpGet("{id:guid}/candidates")]
    public async Task<IActionResult> SearchCandidates(Guid id, [FromQuery] string? query)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.SearchCandidatesAsync(id, userId.Value, query);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMembers(Guid id, [FromBody] AddGroupMembersRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.AddMembersAsync(id, userId.Value, new AddGroupMembersCommand(request.UserIds));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/members/course")]
    public async Task<IActionResult> AddCourseMembers(Guid id, [FromBody] AddGroupCourseRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.AddCourseMembersAsync(id, userId.Value, new AddGroupCourseCommand(request.CourseCode));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.RemoveMemberAsync(id, currentUserId.Value, userId);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> SetMemberRole(Guid id, Guid userId, [FromBody] SetGroupMemberRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.SetMemberRoleAsync(id, currentUserId.Value, userId, new SetGroupMemberRoleCommand(request.Role));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> JoinGroup(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.JoinGroupAsync(id, userId.Value);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> LeaveGroup(Guid id, [FromBody] LeaveGroupRequest? request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.LeaveGroupAsync(id, userId.Value, new LeaveGroupCommand(request?.NewOwnerUserId));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/requests/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(Guid id, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.ApproveJoinRequestAsync(id, currentUserId.Value, userId);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/requests/{userId:guid}/reject")]
    public async Task<IActionResult> RejectJoinRequest(Guid id, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.RejectJoinRequestAsync(id, currentUserId.Value, userId);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/invitations")]
    public async Task<IActionResult> InviteMembers(Guid id, [FromBody] InviteGroupMembersRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.InviteMembersAsync(id, userId.Value, new InviteGroupMembersCommand(request.UserIds));
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/invitations/{userId:guid}")]
    public async Task<IActionResult> CancelInvitation(Guid id, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.CancelInvitationAsync(id, currentUserId.Value, userId);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/invitations/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.RespondToInvitationAsync(id, userId.Value, accept: true);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/invitations/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var result = await groupsService.RespondToInvitationAsync(id, userId.Value, accept: false);
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
