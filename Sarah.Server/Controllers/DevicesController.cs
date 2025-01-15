using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models.Dtos;
using Sarah.Logging;

namespace Sarah.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] 
public class DevicesController(IDeviceService _deviceService, IDBService _databaseService) : ControllerBase
{

    [HttpGet]
    public ActionResult<IEnumerable<DeviceDto>> GetDevicesAsync()
    {
        // Get all devices
        IEnumerable<DeviceDto> dtos = CreateDeviceDtos(_deviceService, _databaseService);
        return Ok(dtos);
    }


    private static IEnumerable<DeviceDto> CreateDeviceDtos(IDeviceService deviceService, IDBService database)
    {
        List<DeviceDto> result = new List<DeviceDto>();
        try
        {
            database.Devices.ToList().ForEach(device =>
            {
                var dto = new DeviceDto() 
                { 
                    Name = device.Name, 
                    Info = (device.GetNetworkItem(deviceService)?.StateInfo) ?? "Unknown state    ", 
                    TypeName = device.SpecificType.ToString() + "|" + (device.GetNetworkItem(deviceService)?.GetType().Name ?? "Unknown type"), 
                    ID = device.Id,
                    NodeID = device.NodeID,
                    DeviceType = device.SpecificType,
                    RoomId = device.Id_Room
                };
                result.Add(dto);
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.LogDebug(ex.Message);
            result.Add(new DeviceDto() { Name = "Fehler", Info = ex.Message });
        }

        return result
            .OrderBy(x => x.ID);
    }
}
