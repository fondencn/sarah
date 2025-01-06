using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] 
public class DevicesController(IDeviceService _deviceService, IDBService _databaseService) : ControllerBase
{

    [HttpGet]
    public ActionResult<IEnumerable<NetworkElementDto>> GetDevicesAsync()
    {
        // Get all devices
        IEnumerable<NetworkElementDto> dtos = NetworkElementFactory.Create
            (_deviceService, _databaseService);
        return Ok(dtos);
    }
}
