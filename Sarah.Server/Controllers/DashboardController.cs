using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class DashboardController(IDBService _databaseService, IDeviceService _deviceService) : ControllerBase
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
                        var networkElement =  device?.GetNetworkItem(_deviceService);
                        JsonObject extendedProperties = ReadObjPropertiesAsJson(networkElement);
                        DashboardItemDto item = new DashboardItemDto()
                        {
                            ItemId = device?.Id ?? 0,
                            ItemType = (DashboardItemTypeDto)favourite.ItemType,
                            Subtype = networkElement?.ClassDescription ?? String.Empty,
                            Title = device?.Name ?? "Unknown Device",
                            Description = networkElement?.StateInfo ?? String.Empty,
                            ExtendedProperties = extendedProperties
                        };
                        list.Add(item);
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
        ///  Read the public, non-static properties of an object and return them as a JsonObject
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private JsonObject ReadObjPropertiesAsJson(object? device)
        {
            JsonObject result = new JsonObject();

            if (device != null) 
            {
                foreach(var property in device.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                   result.Add(property.Name, property.GetValue(device)?.ToString());
                }
            }

            return result;
        }
    }
}