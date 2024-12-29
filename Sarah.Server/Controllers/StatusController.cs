using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public ActionResult GetStatus()
        {
            var hostname = Dns.GetHostName();
            var port = HttpContext.Connection.LocalPort;
            var isAuthenticated = User.Identity?.IsAuthenticated == true;

            var status = new
            {
                Hostname = hostname,
                Port = port,
                IsAuthenticated = isAuthenticated
            };

            return Ok(status);
        }
    }
}