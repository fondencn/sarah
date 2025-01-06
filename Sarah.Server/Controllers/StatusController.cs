using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.Server.Models.Dtos;
using System.Net;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public ActionResult<StatusDto> GetStatus(IDeviceService deviceService)
        {
            var hostname = Dns.GetHostName();
            var port = HttpContext.Connection.LocalPort;
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var username = User.Identity?.Name;
            
            var status = new StatusDto
            {
                Hostname = hostname,
                Port = port,
                IsAuthenticated = isAuthenticated,
                Username = username,
                ControllerStatus = deviceService.StatusMessage
            };


            return Ok(status);
        }
    }
}