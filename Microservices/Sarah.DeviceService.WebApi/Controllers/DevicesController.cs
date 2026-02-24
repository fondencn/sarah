using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.WebApi.Data;
using Sarah.DeviceService.WebApi.DTOs;
using Sarah.API.BusinessObjects.DTOs;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using System.Linq;
using Sarah.API.Interfaces;

namespace Sarah.DeviceService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
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

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _dbContext.Devices.ToListAsync(cancellationToken);
            var dtos = devices.Select(d => new DeviceDto
            {
                Id = d.Id,
                RoomId = d.Id_Room,
                Name = d.Name,
                NodeId = d.NodeID,
                DeviceType = d.SpecificType,
                TypeName = d.SpecificType.ToString(),
                IsReadonly = d.IsReadonly,
                IsFavourite = d.IsFavourite
            }).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all devices");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut]
    public async Task<IActionResult> Create([FromBody] DeviceDto? deviceDto, CancellationToken cancellationToken)
    {
        var validationError = ValidateDeviceDto(deviceDto);
        if (validationError != null) return validationError;
        try
        {
            var entity = new Data.Entities.DeviceInfoEntity
            {
                Name = deviceDto!.Name,
                NodeID = (byte)deviceDto.NodeId,
                Id_Room = deviceDto.RoomId,
                SpecificType = deviceDto.DeviceType,
                IsReadonly = deviceDto.IsReadonly,
                IsFavourite = deviceDto.IsFavourite
            };
            _dbContext.Devices.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            deviceDto.Id = entity.Id;
            deviceDto.TypeName = entity.SpecificType.ToString();
            return Ok(deviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] DeviceDto? deviceDto, CancellationToken cancellationToken)
    {
        var validationError = ValidateDeviceDto(deviceDto);
        if (validationError != null) return validationError;
        try
        {
            var entity = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceDto!.Id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }
            entity.Name = deviceDto!.Name;
            entity.NodeID = (byte)deviceDto.NodeId;
            entity.Id_Room = deviceDto.RoomId;
            entity.SpecificType = deviceDto.DeviceType;
            entity.IsReadonly = deviceDto.IsReadonly;
            entity.IsFavourite = deviceDto.IsFavourite;
            await _dbContext.SaveChangesAsync(cancellationToken);
            deviceDto.TypeName = entity.SpecificType.ToString();
            return Ok(deviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId}", deviceDto!.Id);
            return StatusCode(500, "Internal server error");
        }
    }

    private BadRequestObjectResult? ValidateDeviceDto(DeviceDto? deviceDto)
    {
        if (deviceDto == null || string.IsNullOrWhiteSpace(deviceDto.Name))
            return BadRequest("Device name is required");
        if (deviceDto.NodeId < 0 || deviceDto.NodeId > 255)
            return BadRequest("NodeId must be between 0 and 255");
        return null;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }
            _dbContext.Devices.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/favourite/{isFavourite}")]
    public async Task<IActionResult> SetFavourite(long id, bool isFavourite, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }
            entity.IsFavourite = isFavourite;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting favourite for device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpGet("lamps")]
    public IActionResult GetLamps()
    {
        try
        {
            var lamps = _deviceService.Lamps.Select(l => l.ToDto()).ToList();
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
            var sensors = _deviceService.Sensors.Select(s => s.ToDto()).ToList();
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
            var doorSensors = _deviceService.DoorSensors.Select(d => d.ToDto()).ToList();
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
            var wallPlugs = _deviceService.WallPlugs.Select(w => w.ToDto()).ToList();
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
            var thermostats = _deviceService.Heatings.Select(h => h.ToDto()).ToList();
            return Ok(thermostats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thermostats");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("controllers")]
    public IActionResult GetControllers()
    {
        try
        {
            var controllers = _deviceService.Controllers.Select(c => c.ToDto()).ToList();
            return Ok(controllers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controllers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("wallcontrollers")]
    public IActionResult GetWallControllers()
    {
        try
        {
            var wallControllers = _deviceService.WallControllers.Select(w => w.ToDto()).ToList();
            return Ok(wallControllers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wall controllers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("smokesensors")]
    public IActionResult GetSmokeSensors()
    {
        try
        {
            var smokeSensors = _deviceService.SmokeSensors.Select(s => s.ToDto()).ToList();
            return Ok(smokeSensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving smoke sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("batterysensors")]
    public IActionResult GetBatterySensors()
    {
        try
        {
            var batterySensors = _deviceService.BatterySensors.Select(b => b.ToDto()).ToList();
            return Ok(batterySensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving battery sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("unknown")]
    public IActionResult GetUnknownElements()
    {
        try
        {
            var unknowns = _deviceService.UnknownElements.Select(u => u.ToDto()).ToList();
            return Ok(unknowns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unknown elements");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("gps-trackers")]
    public IActionResult GetGpsTrackers()
    {
        try
        {
            var trackers = _deviceService.GPSTrackers.Select(t => t.ToDto()).ToList();
            return Ok(trackers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GPS trackers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", message = _deviceService.StatusMessage });
    }

    [HttpGet("serial-port")]
    public IActionResult GetSerialPortName()
    {
        try
        {
            return Ok(_deviceService.SerialPortName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving serial port name");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("elements")]
    public IActionResult GetNetworkElements()
    {
        try
        {
            var elements = _deviceService.Elements.Select(e => new
            {
                id = (int)e.NodeID,
                type = e.ClassDescription ?? e.StateInfo ?? e.NodeID.ToString()
            }).ToList();
            return Ok(elements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network elements");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("trackers")]
    public IActionResult GetTrackers()
    {
        try
        {
            var trackers = _deviceService.GPSTrackers.Select(t => new
            {
                id = (int)t.NodeID,
                name = t.ClassDescription ?? t.StateInfo ?? $"Tracker {t.NodeID}"
            }).ToList();
            return Ok(trackers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trackers");
            return StatusCode(500, "Internal server error");
        }
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
                NodeId = device.NodeID,
                DeviceType = device.SpecificType,
                TypeName = device.SpecificType.ToString(),
                IsReadonly = device.IsReadonly,
                IsFavourite = device.IsFavourite,
                ExtendedProperties = BuildExtendedProperties(device.NodeID)
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private List<ExtendedPropertyDto> BuildExtendedProperties(byte nodeId)
    {
        var props = new List<ExtendedPropertyDto>();
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is ILamp lamp)
        {
            props.Add(new ExtendedPropertyDto { Key = "IsOn", Value = (lamp.Brightness > 0).ToString() });
            props.Add(new ExtendedPropertyDto { Key = "Color", Value = lamp.Color });
            props.Add(new ExtendedPropertyDto { Key = "Brightness", Value = lamp.Brightness.ToString() });
        }
        else if (networkItem is IWallPlug wallPlug)
        {
            props.Add(new ExtendedPropertyDto { Key = "IsOn", Value = wallPlug.IsOn.ToString() });
            if (networkItem is Sarah.DeviceService.Model.WallPlug concreteWallPlug && concreteWallPlug.Meter_W != null)
            {
                props.Add(new ExtendedPropertyDto { Key = "Meter_W", Value = concreteWallPlug.Meter_W.Value.ToString() });
            }
        }
        return props;
    }

    [HttpGet("node/{nodeId}")]
    public IActionResult GetNode(byte nodeId)
    {
        try
        {
            var node = _deviceService.GetNode(nodeId);
            if (node == null)
            {
                return NotFound();
            }

            return Ok(node.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving node {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("networkitem/{nodeId}")]
    public IActionResult GetNetworkItem(byte nodeId)
    {
        try
        {
            var item = _deviceService.GetNetworkItem(nodeId);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network item {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpGet("nodes/{nodeId}/association-groups")]
    public async Task<IActionResult> GetAssociationGroups(byte nodeId)
    {
        try
        {
            var groups = await _deviceService.GetAssociationGroups(nodeId);
            var dto = groups.Select(g => g.ToDto()).ToList();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving association groups for node {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    public record SetAssociationGroupRequest(byte[] NodeIds);

    [HttpPost("nodes/{nodeId}/association-groups/{groupId}")]
    public async Task<IActionResult> SetAssociationGroup(byte nodeId, byte groupId, [FromBody] SetAssociationGroupRequest request)
    {
        try
        {
            await _deviceService.SetAssociationGroup(nodeId, groupId, request.NodeIds);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting association group {GroupId} for node {NodeId}", groupId, nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("gpstracker/{nodeId}")]
    public async Task<IActionResult> GetGpsTrackerByNodeId(byte nodeId)
    {
        try
        {
            var tracker = await _deviceService.GetGpsTrackerByNodeId(nodeId);
            if (tracker == null)
            {
                return NotFound();
            }

            return Ok(tracker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GPS tracker with NodeID {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }
}
