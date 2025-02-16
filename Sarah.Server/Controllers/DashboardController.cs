using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualBasic;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController(IDBService _databaseService, IDeviceService _deviceService, IPersonService _personService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DashboardItemDto>>> GetDashboard()
        {
            if (User.Identity?.Name == null)
            {
                return Unauthorized();
            }
            List<DashboardItemDto> dashboardItems = await GetDashboardItemsForUser(User.Identity.Name);
            return dashboardItems;
        }

        private async Task<List<DashboardItemDto>> GetDashboardItemsForUser(string username)
        {
            /* Get all favourites for the user */
            var userFavourites = await _databaseService.UserFavourites
                .Where(f => f.UserId == username)
                .ToListAsync();

            /* Create a list of dashboard items based on the user's favourites */
            var list = new List<DashboardItemDto>();
            if (userFavourites.Any())
            {
                foreach (var favourite in userFavourites)
                {
                    if(favourite.ItemType == API.BusinessObjects.DashboardItemType.Device)
                    {
                        var device = await _databaseService.Devices
                            .Where(d => d.Id == favourite.ItemId)
                            .FirstOrDefaultAsync();
                        if(device != null)
                        {
                            var networkElement =  device.GetNetworkItem(_deviceService);
                            var room = await _databaseService.Rooms.FirstOrDefaultAsync(r => r.Id == device.Id_Room);
                            var extendedProperties = ReadObjPropertiesAsJson(networkElement);
                            var description = (networkElement?.ClassDescription ?? "Unknown device type") + " in " + (room?.Name ?? "unknown room");
                            
                            DashboardItemDto item = new DashboardItemDto()
                            {
                                ItemId = device.Id,
                                ItemType = (DashboardItemTypeDto)favourite.ItemType,
                                Subtype = networkElement?.ClassDescription ?? String.Empty,
                                Title = device.Name ?? "Unknown Device",
                                Description = description,
                                ExtendedProperties = extendedProperties
                            };
                        list.Add(item);
                        }
                    }
                    else if(favourite.ItemType == API.BusinessObjects.DashboardItemType.Person)
                    {
                        var person = await _personService.GetPersonByIdAsync(favourite.ItemId);
                        if(person != null)
                        {
                            var extendedProperties = ReadObjPropertiesAsJson(person);
                            string geofenceInfo =  " at " + person.CurrentGeoFence?.Name + " (" + person.GPSTracker?.Position + ")";
                            DashboardItemDto item = new DashboardItemDto()
                            {
                                ItemId = person.Id,
                                ItemType = (DashboardItemTypeDto)favourite.ItemType,
                                Subtype = String.Empty,
                                Title = person.Name ?? "Unknown Person",
                                Description = person.IsAtHome ? "At home" : "Away" + geofenceInfo,
                                ExtendedProperties = extendedProperties
                            };
                        list.Add(item);
                        }
                    }

                    else if(favourite.ItemType == API.BusinessObjects.DashboardItemType.Room)
                    {
                        var room = await _databaseService.Rooms
                            .Where(d => d.Id == favourite.ItemId)
                            .FirstOrDefaultAsync();
                        if(room != null)
                        {
                            var extendedProperties = ReadObjPropertiesAsJson(room);
                            var deviceCount = _databaseService.Devices.Count(d => d.Id_Room == room.Id);
                            var avgTemp = _databaseService.Devices.Where(d => d.Id_Room == room.Id)
                                .ToList()
                                .Select(item => item.GetNetworkItem(_deviceService))
                                .OfType<ITemperatureSensor>()
                                .Select(d => d.Temperature.Value)
                                .DefaultIfEmpty(0)
                                .Average();
                            var presence = _databaseService.Devices.Where(d => d.Id_Room == room.Id)
                                .ToList()
                                .Select(item => item.GetNetworkItem(_deviceService))
                                .OfType<IMultiSensor>()
                                .Select(d => d.Presence.Value)
                                .Any(item => item != 0) 
                                ? "| 👤" : "";
                            string roomStatusInfo = $"{deviceCount} devices | {avgTemp}°C {presence}";
                            DashboardItemDto item = new DashboardItemDto()
                            {
                                ItemId = room.Id,
                                ItemType = (DashboardItemTypeDto)favourite.ItemType,
                                Subtype = String.Empty,
                                Title = room.Name ?? "Unknown Room",
                                Description = roomStatusInfo,
                                ExtendedProperties = extendedProperties
                            };
                        list.Add(item);
                        }
                    }
                    /* TODO: Generate more dashboard items for other types of favourites */
                }
            }
            else
            {
                list.Add(DashboardItemDto.Default);
            }
            return list;
        }

        /// <summary>
        ///  Read the public, non-static properties of an object and return them as a ExtendedPropertyDto[]
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private ExtendedPropertyDto[] ReadObjPropertiesAsJson(object? item)
        {
            List<ExtendedPropertyDto> result = new List<ExtendedPropertyDto>();

            if (item != null) 
            {
                foreach(var property in item.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                   result.Add(new ExtendedPropertyDto() {Key = property.Name, Value = property.GetValue(item)?.ToString()  ?? ""});
                }
            }

            return result.ToArray();
        }
    }
}