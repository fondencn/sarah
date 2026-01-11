using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;

namespace Sarah.DeviceService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpGet("lamps")]
    public IActionResult GetLamps()
    {
        try
        {
            var lamps = _deviceService.Lamps;
            return Ok(lamps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lamps");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("sensors")]
    public IActionResult GetSensors()
    {
        try
        {
            var sensors = _deviceService.Sensors;
            return Ok(sensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("doorsensors")]
    public IActionResult GetDoorSensors()
    {
        try
        {
            var doorSensors = _deviceService.DoorSensors;
            return Ok(doorSensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving door sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("wallplugs")]
    public IActionResult GetWallPlugs()
    {
        try
        {
            var wallPlugs = _deviceService.WallPlugs;
            return Ok(wallPlugs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wall plugs");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("thermostats")]
    public IActionResult GetThermostats()
    {
        try
        {
            var thermostats = _deviceService.Heatings;
            return Ok(thermostats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thermostats");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", message = _deviceService.StatusMessage });
    }
}
