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
            // Check if the Authorization header is present
            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Log or inspect the Authorization header
                var token = authHeader.ToString();
                Console.WriteLine($"Authorization Header: {token}");
            }
            else
            {
                Console.WriteLine("Authorization Header is missing.");
            }

            var hostname = Dns.GetHostName();
            var port = HttpContext.Connection.LocalPort;
            var isAuthenticated = User.Identity?.IsAuthenticated == true;

            var status = new StatusDto
            {
                Hostname = hostname,
                Port = port,
                IsAuthenticated = isAuthenticated
            };

            return Ok(status);
        }
    }
}