using CampusConnect.API.Common;
using CampusConnect.Application.Features.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/contacts")]
public class ContactsController(ContactsService contactsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchContacts([FromQuery] string? query, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { error = "User could not be resolved from the token." });

        var contacts = await contactsService.SearchAsync(userId.Value, query, limit, cancellationToken);
        return Ok(contacts);
    }
}
