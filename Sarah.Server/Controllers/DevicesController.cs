using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models.Dtos;
using Sarah.Logging;
using ZWave.Devices;
using Sarah.Data.Models;
using Microsoft.EntityFrameworkCore;

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
            IEnumerable<DeviceDto> dtos = (await _databaseService.Devices
                .ToListAsync())
                .Select(item => item.ToDto(_deviceService))
                .OrderBy(x => x.NodeID);
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
        return Ok(entity.ToDto(_deviceService));
    }

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> UpdateDevice([FromBody] DeviceDto device)
    {
        // Update a device

        if (device == null)
        {
            return BadRequest("Device cannot be null");
        }
        
        DeviceInfo? entity = _databaseService.Devices.Find(device.ID);
        if (entity == null)
        {
            return NotFound("Device not found");
        }
        entity.UpdateFromDto(device);
        await _databaseService.SaveChangesAsync();
        return Ok(entity.ToDto(_deviceService));
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
}
