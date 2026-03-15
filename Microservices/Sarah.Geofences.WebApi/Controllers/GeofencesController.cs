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
                new API.Business.SensorData(longitude, "°"),
                new API.Business.SensorData(latitude, "°")
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
            var geofences = _geoFenceService.GetAll()
                .OfType<Sarah.Geofences.GeoFence>()
                .Select(gf => new
                {
                    name = gf.Name,
                    points = gf.Points.Select(p => new
                    {
                        latitude  = new { value = p.Latitude.Value },
                        longtitude = new { value = p.Longtitude.Value }
                    })
                });
            return Ok(geofences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all geofences");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("wellknownlocations")]
    public IActionResult GetWellKnownLocations()
    {
        try
        {
            var home = _geoFenceService.GetZuhause() as Sarah.Geofences.GeoFence;
            if (home?.Points == null || home.Points.Length == 0)
                return Ok(Array.Empty<object>());

            var lat = home.Points.Average(p => (double)p.Latitude.Value);
            var lng = home.Points.Average(p => (double)p.Longtitude.Value);

            return Ok(new[] { new { latitude = lat, longitude = lng, name = home.Name } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving well-known locations");
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
