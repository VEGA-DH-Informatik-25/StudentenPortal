using CampusConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[ApiController]
[Route("api/mensa")]
public class MensaController(IMensaService mensaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await mensaService.GetWeekMenuAsync();
        return Ok(menu);
    }
}
