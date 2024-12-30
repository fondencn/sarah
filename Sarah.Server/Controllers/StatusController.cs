using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.Server.Models.Dtos;
using System.Net;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public ActionResult<StatusDto> GetStatus()
        {
            var hostname = Dns.GetHostName();
            var port = HttpContext.Connection.LocalPort;
            var isAuthenticated = User.Identity?.IsAuthenticated == true;

            StatusDto status = new StatusDto()
            {
                Hostname = hostname,
                Port = port,
                IsAuthenticated = isAuthenticated
            };

            return Ok(status);
        }
    }
}