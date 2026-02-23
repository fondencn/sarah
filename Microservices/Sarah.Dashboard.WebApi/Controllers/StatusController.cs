using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.Dashboard.WebApi.DTOs;

namespace Sarah.Dashboard.WebApi.Controllers;

/// <summary>
/// Controller for reporting Dashboard service status
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;

    public StatusController(ILogger<StatusController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the current status of the Dashboard service and the authenticated user
    /// </summary>
    /// <returns>Status information including hostname, port, authentication state and username</returns>
    [HttpGet]
    [ProducesResponseType(typeof(StatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetStatus()
    {
        var request = HttpContext.Request;

        var hostname = request.Host.Host;
        var port = request.Host.Port ?? (request.IsHttps ? 443 : 80);

        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var username = User.Identity?.Name;

        _logger.LogInformation("Status requested by user {Username}", username);

        var status = new StatusDto
        {
            Hostname = hostname,
            Port = port,
            IsAuthenticated = isAuthenticated,
            Username = username,
            ControllerStatus = "Running"
        };

        return Ok(status);
    }
}
