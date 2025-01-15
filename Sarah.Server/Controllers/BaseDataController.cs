using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers
{
    /// <summary>
    /// Controller for base data to be cache in the app like rooms, device types and network elements
    /// </summary>
    /// <param name="database"></param>
    /// <param name="deviceService"></param>
    [ApiController]
    [Authorize] 
    [Route("api/[controller]")]
    public class BaseDataController(IDBService database, IDeviceService deviceService) : ControllerBase
    {
        // GET: api/basedata
        [HttpGet]
        public async Task<ActionResult<BaseDataDto>> Get()
        {
            BaseDataDto result = new BaseDataDto();
            result.DeviceTypeEnumeration = CreateEnumDescription<KnownDeviceTypes>();
            
            result.Rooms = await database.Rooms
                .Select(r => new RoomDto { Id = r.Id, Name = (r.Name ?? String.Empty) })
                .ToArrayAsync();
            result.NetworkElements = deviceService.Elements
                .Select(e => new NetworkElementDto { Id = e.NodeID, Type = e.GetType().Name })
                .ToArray();

            return result;
        }

        private static EnumDto[] CreateEnumDescription<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new EnumDto { EnumKey = Convert.ToInt32(e), EnumValue = e.ToString() })
                .ToArray();
        }
    }
}