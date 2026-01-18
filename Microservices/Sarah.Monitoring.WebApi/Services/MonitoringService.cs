using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Monitoring.WebApi.Data;

namespace Sarah.Monitoring;

public class MonitoringService (ApplicationDbContext _db, IDeviceService _devices, IEventProcessingService _events, IConfiguration _config, IRuleService _rules, ILoggerFactory _loggerFactory) : IMonitoringService
{
    private  IMonitor[] Monitors {get; set;} = Array.Empty<IMonitor>();

    public Task Start()
    {
        var weather = new Monitors.WeatherMonitor(_config, _events, _loggerFactory.CreateLogger<Monitors.WeatherMonitor>());
        weather.WarnLocation = _config["WeatherWarnLocation"] ?? "Berlin";
        var ferien = new Monitors.FerienMonitor(_config, _loggerFactory.CreateLogger<Monitors.FerienMonitor>());
        var doors = new Monitors.DoorMonitor(_db, _events, _devices, weather, _loggerFactory.CreateLogger<Monitors.DoorMonitor>());
        Monitors = new IMonitor[]
        {
            new Monitors.AirQualityMonitor(_db, _events, _devices, _loggerFactory.CreateLogger<Monitors.AirQualityMonitor>()),
            new Monitors.PersonMonitor(_db, _events, _loggerFactory.CreateLogger<Monitors.PersonMonitor>()),
            new Monitors.BatteryMonitor(_db, _devices, _events, _config, _loggerFactory.CreateLogger<Monitors.BatteryMonitor>()), 
            doors,
            ferien, 
            weather,
            new Monitors.RuleMonitor(_rules, ferien, _events, _db, _devices, doors, weather, _loggerFactory.CreateLogger<Monitors.RuleMonitor>()),
        };

        return Task.WhenAll(Monitors.Select(m => m.Start()).ToArray());
    }

    public IWeatherProvider Weather => Monitors.OfType<IWeatherProvider>().First();
    public IFerienInfoProvider Ferien => Monitors.OfType<IFerienInfoProvider>().First();
}
