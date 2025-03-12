using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models.Dtos;
using Sarah.Logging;
using ZWave.Devices;
using Sarah.Data.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;

namespace Sarah.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DevicesController(IDeviceService _deviceService, IDBService _databaseService) : ControllerBase
{
    private string CurrentUserName => User.Identity?.Name ?? "";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetDevicesAsync()
    {
        try
        {
            // Get all devices
            List<DeviceDto> dtos = (await _databaseService.Devices.ToListAsync())
                .Select(item => item.ToDto(_deviceService))
                .OrderBy(x => x.NodeId)
                .ToList();

            var userFavourites = await _databaseService.UserFavourites
                .Where(f => f.UserId == CurrentUserName)
                .ToListAsync();
            foreach(var dto in dtos)
            {
                dto.IsFavourite = userFavourites.Any(x => x.ItemId == dto.Id && x.ItemType == DashboardItemType.Device);
            }   
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Logger.Instance.LogException("Error getting devices", ex);
            return StatusCode(500, "Error getting devices");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceDto>> GetDeviceAsync(long id)
    {
        try
        {
            // Get all devices
            DeviceDto dto = (await _databaseService.Devices.FindAsync(id))
                !.ToDto(_deviceService);

            var userFavourites = await _databaseService.UserFavourites
                .Where(f => f.UserId == CurrentUserName)
                .ToListAsync();

            dto.IsFavourite = userFavourites.Any(x => x.ItemId == dto.Id && x.ItemType == DashboardItemType.Device);
              
            return Ok(dto);
        }
        catch (Exception ex)
        {
            Logger.Instance.LogException("Error getting device " + id, ex);
            return StatusCode(500, "Error getting device " + id);
        }
    }

    [HttpPut]
    public async Task<ActionResult<DeviceDto>> AddDevice([FromBody] DeviceDto device)
    {
        // Add a device
        if (device == null)
        {
            return BadRequest("Device cannot be null");
        }

        DeviceInfo entity = device.ToEntity();
        await _databaseService.Devices.AddAsync(entity);
        await _databaseService.SaveChangesAsync();
        var dto = entity.ToDto(_deviceService);
        
        var userFavourites = await _databaseService.UserFavourites
            .Where(f => f.UserId == CurrentUserName)
            .ToListAsync();
        dto.IsFavourite = userFavourites.Any(x => x.ItemId == dto.Id && x.ItemType == DashboardItemType.Device);

        
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> UpdateDevice([FromBody] DeviceDto device)
    {
        // Update a device

        if (device == null)
        {
            return BadRequest("Device cannot be null");
        }

        DeviceInfo? entity = _databaseService.Devices.Find(device.Id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        entity.UpdateFromDto(device);
        await _databaseService.SaveChangesAsync();
        var dto = entity.ToDto(_deviceService);
        
        var userFavourites = await _databaseService.UserFavourites
            .Where(f => f.UserId == CurrentUserName)
            .ToListAsync();
        dto.IsFavourite = userFavourites.Any(x => x.ItemId == dto.Id && x.ItemType == DashboardItemType.Device);

        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDevice(long id)
    {
        // Delete a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        _databaseService.Devices.Remove(entity);
        await _databaseService.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}/favourite/{isFavourite}")]
    public async Task<ActionResult> SetFavourite(long id, bool isFavourite)
    {
        // Set a device as favourite
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
            
        var existing = await _databaseService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Device);

        if (isFavourite)
        {
            if (existing != null)
            {
                return BadRequest("Device already marked as favourite");
            }
            _databaseService.UserFavourites.Add(new UserFavourite
            {
                ItemId = id,
                UserId = CurrentUserName,
                ItemType = DashboardItemType.Device
            });
        }
        else
        {
            if (existing != null)
            {
                _databaseService.UserFavourites.Remove(existing);
            }
        }
        
        await _databaseService.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("lamp/{id}/brightness/{brightness}")]
    public async Task<ActionResult> SetLampBrightness(long id, byte brightness)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var lamp = _deviceService.Lamps.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (lamp == null)
        {
            return NotFound("Device is not a lamp");
        }
        await lamp.SetBrightness(brightness); 
        
        return Ok();
    }

    [HttpPost("lamp/{id}/color/{color}")]
    public async Task<ActionResult> SetLampColor(long id, string color)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var lamp = _deviceService.Lamps.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (lamp == null)
        {
            return NotFound("Device is not a lamp");
        }
        await lamp.SetColor(color);
        
        return Ok();
    }

    [HttpGet("lamp/{id}/color")]
    public ActionResult GetLampColor(long id)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var lamp = _deviceService.Lamps.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (lamp == null)
        {
            return NotFound("Device is not a lamp");
        }

        return Ok(lamp.Color);
    }

    [HttpPost("lamp/{id}/warmwhite")]
    public async Task<ActionResult> SetLampWarmWhite(long id)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var lamp = _deviceService.Lamps.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (lamp == null)
        {
            return NotFound("Device is not a lamp");
        }
        await lamp.SetWarmWhite();
        
        return Ok();
    }

    [HttpPost("lamp/{id}/coldwhite")]
    public async Task<ActionResult> SetLampColdWhite(long id)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var lamp = _deviceService.Lamps.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (lamp == null)
        {
            return NotFound("Device is not a lamp");
        }
        await lamp.SetColdWhite();
        
        return Ok();
    }

    [HttpPost("wallplug/{id}/{isOn}")]
    public async Task<ActionResult> SetWallplugOnOff(long id, bool isOn)
    {
        // Set the state of a device
        DeviceInfo? entity = _databaseService.Devices.Find(id);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        var wallplug = _deviceService.WallPlugs.FirstOrDefault(x => x.NodeID == entity.NodeID);
        if (wallplug == null)
        {
            return NotFound("Device is not a wallplug");
        }
        await wallplug.SetState(isOn);
        
        return Ok();
    }
}
