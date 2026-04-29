using CampusConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[Authorize]
[ApiController]
[Route("api/mensa")]
public class MensaController(IMensaService mensaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMenu(CancellationToken cancellationToken)
    {
        try
        {
            var menu = await mensaService.GetWeekMenuAsync(cancellationToken);
            return Ok(menu);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
