using Microsoft.AspNetCore.Mvc;
using Sarah.Server.Models;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DevicesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetDevicesAsync()
    {
        // Get all devices

        IEnumerable<NetworkElementDto> dtos = NetworkElementFactory.Create();
        return Ok(dtos);
    }
}
