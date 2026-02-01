using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;

namespace Sarah.Monitoring.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly MonitoringService _monitoringService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(MonitoringService monitoringService, ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    [HttpGet("weather")]
    public IActionResult GetWeather()
    {
        try
        {
            var weather = _monitoringService.Weather;
            
            if (weather == null)
            {
                return NotFound("Weather data not available");
            }

            return Ok(new
            {
                temperature = weather.CurrentOutdoorTemperature,
                weatherString = weather.GetCurrentWeatherString(),
                forecast = weather.GetWeatherForecastStringForToday(),
                warnings = weather.GetWeatherWarningString(),
                sunrise = weather.GetSunrise()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weather");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("ferien")]
    public IActionResult GetFerien()
    {
        try
        {
            var ferien = _monitoringService.Ferien;
            var aktuelleFerien = ferien?.AktuelleFerien;
            
            return Ok(new
            {
                aktuelleFerien = aktuelleFerien?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ferien info");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", service = "MonitoringService" });
    }
}
