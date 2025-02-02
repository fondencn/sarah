using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
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
                        DashboardItemDto item = new DashboardItemDto()
                        {
                            ItemId = device?.Id ?? 0,
                            ItemType = (DashboardItemTypeDto)favourite.ItemType,
                            Subtype = device?.GetNetworkItem(_deviceService)?.GetType().Name ?? String.Empty,
                            Title = device?.Name ?? "Unknown Device",
                            Description = device?.GetNetworkItem(_deviceService)?.StateInfo ?? String.Empty,
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
    }
}