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
    public class BaseDataController(IDBService database, IDeviceService deviceService, IPersonService persons) : ControllerBase
    {


        private string CurrentUserName => User.Identity?.Name ?? "";


        // GET: api/basedata
        [HttpGet]
        public async Task<ActionResult<BaseDataDto>> Get()
        {
            BaseDataDto result = new BaseDataDto();
            result.DeviceTypeEnumeration = CreateEnumDescription<KnownDeviceTypes>();
            
            result.Rooms = (await database.Rooms
                .ToListAsync())
                .Select(r => r.ToDto(database.UserFavourites.FirstOrDefault(x => x.ItemId == r.Id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Room) != null))
                .ToArray();

            result.NetworkElements = deviceService.Elements
                .Select(e => e.ToDto())
                .ToArray();

            result.MobilePhones = await persons.GetMobilePhones();
            var trackerIds = deviceService.GPSTrackers.Select(item => item.NodeID);
            var trackerDevices = await database.Devices
                .ToArrayAsync();
            result.Trackers =  trackerDevices
                .Where(d => trackerIds.Contains(d.NodeID))
                .Select(d => new TrackerDto() { Id = d.Id, Name = d.Name  ?? ""})
                .ToArray();

            return result;
        }

        private static EnumDto[] CreateEnumDescription<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => e.ToDto())
                .ToArray();
        }
    }
}