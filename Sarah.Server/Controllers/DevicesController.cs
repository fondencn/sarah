using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] 
public class DevicesController : ControllerBase
{
    private IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }   

    [HttpGet]
    public ActionResult<IEnumerable<NetworkElementDto>> GetDevicesAsync()
    {
        // Get all devices
        IEnumerable<NetworkElementDto> dtos = NetworkElementFactory.Create(_deviceService, out string StatusMessage);
        return Ok(dtos);
    }
}
