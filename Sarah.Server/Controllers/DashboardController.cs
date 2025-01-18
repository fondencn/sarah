using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
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
            if (username == "fondencn")
            {
                return new List<DashboardItemDto>
                {
                    new DashboardItemDto
                    {
                        ItemId = 1,
                        ItemType = DashboardItemType.Device,
                        Title = "Working Room Light Bulb",
                        Description = "The light in Christian's working room"
                    },
                    new DashboardItemDto
                    {
                        ItemId = 1,
                        ItemType = DashboardItemType.Device,
                        Title = "Working Room LED strip",
                        Description = "The LEDF strip in Christian's working room"
                    },
                    new DashboardItemDto
                    {
                        ItemId = 2,
                        ItemType = DashboardItemType.Scene,
                        Title = "Good Morning",
                        Description = "Turn on the lights and open the blinds"
                    },
                    new DashboardItemDto
                    {
                        ItemId = 3,
                        ItemType = DashboardItemType.Room,
                        Title = "Living Room",
                        Description = "21,5°C | 45% humidity | 2 persons"
                    },
                    new DashboardItemDto
                    {
                        ItemId = 2,
                        ItemType = DashboardItemType.Person,
                        Title = "Christian",
                        Description = "Christian is at home"
                    }
                };
            }
            else
            {
                return new List<DashboardItemDto>();
            }
        }
    }
}