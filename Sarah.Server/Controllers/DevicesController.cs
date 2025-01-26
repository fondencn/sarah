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
                .Where(f => f.UserId == User.Identity.Name)
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
            .Where(f => f.UserId == User.Identity.Name)
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
            .Where(f => f.UserId == User.Identity.Name)
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
            
        var existing = await _databaseService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == id && x.UserId == User.Identity.Name && x.ItemType == DashboardItemType.Device);

        if (isFavourite)
        {
            if (existing != null)
            {
                return BadRequest("Device already marked as favourite");
            }
            _databaseService.UserFavourites.Add(new UserFavourite
            {
                ItemId = id,
                UserId = User.Identity.Name,
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
}
