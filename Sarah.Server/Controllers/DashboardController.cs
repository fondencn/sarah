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
            /* TODO Implement fetching dashboard items for user */
            return new List<DashboardItemDto>(new DashboardItemDto[]{ DashboardItemDto.Default});
        }
    }
}