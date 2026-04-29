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
    public async Task<IActionResult> SearchContacts([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { error = "Benutzer konnte nicht aus dem Token ermittelt werden." });

        var contacts = await contactsService.SearchAsync(userId.Value, query, cancellationToken);
        return Ok(contacts);
    }
}
