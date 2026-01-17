using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;

namespace Sarah.Geofences.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GeofencesController : ControllerBase
{
    private readonly IGeoFenceService _geoFenceService;
    private readonly ILogger<GeofencesController> _logger;

    public GeofencesController(IGeoFenceService geoFenceService, ILogger<GeofencesController> logger)
    {
        _geoFenceService = geoFenceService;
        _logger = logger;
    }

    [HttpGet("current")]
    public IActionResult GetCurrent([FromQuery] float latitude, [FromQuery] float longitude)
    {
        try
        {
            var position = new LocatorPosition(
                new API.Business.SensorData(latitude, "°"),
                new API.Business.SensorData(longitude, "°")
            );
            
            var geofence = _geoFenceService.GetCurrent(position);
            
            if (geofence == null)
            {
                return NotFound("No geofence found for the given position");
            }
            return Ok(geofence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current geofence for position (coordinates redacted for privacy)");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("home")]
    public IActionResult GetHome()
    {
        try
        {
            var geofence = _geoFenceService.GetZuhause();
            return Ok(geofence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving home geofence");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var geofences = _geoFenceService.GetAll();
            return Ok(geofences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all geofences");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", service = "GeoFenceService" });
    }
}
