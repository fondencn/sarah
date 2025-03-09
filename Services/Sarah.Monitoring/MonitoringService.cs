using Microsoft.Extensions.Configuration;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;

namespace Sarah.Monitoring;

public class MonitoringService (IDBService _db, IDeviceService _devices, IEventProcessingService _events, IConfiguration _config, IRuleService _rules) : IMonitoringService
{
    private  IMonitor[] Monitors {get; set;} = Array.Empty<IMonitor>();

    public Task Start()
    {
        var weather = new Monitors.WeatherMonitor(_config, _events);
        weather.WarnLocation = _config["WeatherWarnLocation"] ?? "Berlin";
        var ferien = new Monitors.FerienMonitor(_config);
        var doors = new Monitors.DoorMonitor(_db, _events, _devices, weather);
        Monitors = new IMonitor[]
        {
            new Monitors.AirQualityMonitor(_db, _events, _devices),
            new Monitors.PersonMonitor(_db, _events),
            new Monitors.BatteryMonitor(_db, _devices, _events, _config), 
            doors,
            ferien, 
            weather,
            new Monitors.RuleMonitor(_rules, ferien, _events, _db, _devices, doors, weather),
        };

        return Task.WhenAll(Monitors.Select(m => m.Start()).ToArray());
    }

    public IWeatherProvider Weather => Monitors.OfType<IWeatherProvider>().First();
    public IFerienInfoProvider Ferien => Monitors.OfType<IFerienInfoProvider>().First();
}
