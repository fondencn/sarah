using Microsoft.Extensions.Configuration;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;

namespace Sarah.Monitoring;

public class MonitoringService (IDBService _db, IDeviceService _devices, IEventProcessingService _events, IConfiguration _config) : IMonitoringService
{
    private  IMonitor[] Monitors {get; set;} = Array.Empty<IMonitor>();

    public Task Start()
    {
        var weather = new Monitors.WeatherMonitor(_config, _events);
        Monitors = new IMonitor[]
        {
            new Monitors.AirQualityMonitor(_db, _events, _devices),
            new Monitors.PersonMonitor(_db, _events),
            new Monitors.BatteryMonitor(_db, _devices, _events, _config), 
            new Monitors.DoorMonitor(_db, _events, _devices, weather),
            new Monitors.FerienMonitor(_config), 
            weather
        };

        return Task.WhenAll(Monitors.Select(m => m.Start()).ToArray());
    }
}
