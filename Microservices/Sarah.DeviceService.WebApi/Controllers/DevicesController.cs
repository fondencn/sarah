using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.WebApi.Data;
using Sarah.DeviceService.WebApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Sarah.DeviceService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ApplicationDbContext dbContext, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _dbContext = dbContext;
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeviceById(long id)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound();
            }

            var dto = new DeviceDto
            {
                Id = device.Id,
                RoomId = device.Id_Room,
                Name = device.Name,
                NodeID = device.NodeID,
                DeviceType = device.SpecificType.ToString(),
                IsReadonly = device.IsReadonly
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("gpstracker/{nodeId}")]
    public IActionResult GetGpsTrackerByNodeId(byte nodeId)
    {
        try
        {
            var tracker = _deviceService.GPSTrackers?.FirstOrDefault(t => t.NodeID == nodeId);
            if (tracker == null)
            {
                return NotFound();
            }

            var dto = new DeviceDto
            {
                Id = 0, // Runtime device, not persisted
                RoomId = null,
                Name = $"GPS Tracker {tracker.NodeID}",
                NodeID = tracker.NodeID,
                DeviceType = "GPSTracker",
                IsReadonly = true
            };

            // Add position information if available
            if (tracker.Position != null)
            {
                dto.Position = new PositionDto
                {
                    Longitude = tracker.Position.Longtitude?.Value ?? 0f,
                    Latitude = tracker.Position.Latitude?.Value ?? 0f,
                    MeasureTime = tracker.Position.MeasureTime,
                    IsValid = tracker.Position.IsValid
                };
            }

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GPS tracker with NodeID {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }
}
