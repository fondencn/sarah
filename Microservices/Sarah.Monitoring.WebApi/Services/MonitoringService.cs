using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.Monitoring.WebApi.Data;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Monitoring.Monitors;

namespace Sarah.Monitoring;

public class MonitoringService (IServiceProvider _serviceProvider, IDeviceService _devices, RabbitMQClient _rabbitMQ, IConfiguration _config, ILoggerFactory _loggerFactory, ILogger<MonitoringService> _logger, IHttpClientFactory _httpClientFactory) : BackgroundService
{
    public IWeatherProvider Weather  => this.Monitors.OfType<IWeatherProvider>().FirstOrDefault() ?? throw new InvalidOperationException("No IWeatherProvider monitor available");

    public FerienMonitor Ferien => this.Monitors.OfType<FerienMonitor>().FirstOrDefault() ?? throw new InvalidOperationException("No IFerienInfoProvider monitor available");

    private  IMonitor[] Monitors {get; set;} = Array.Empty<IMonitor>();

    public Task Start()
    {
        // Start is handled by BackgroundService.ExecuteAsync
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var weather = new Monitors.WeatherMonitor(_config, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.WeatherMonitor>(), _httpClientFactory);
        weather.WarnLocation = _config["WeatherWarnLocation"] ?? "Berlin";
        var ferien = new Monitors.FerienMonitor(_config, _loggerFactory.CreateLogger<Monitors.FerienMonitor>(), _rabbitMQ);
        var doors = new Monitors.DoorMonitor(_db, _devices, weather, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.DoorMonitor>());
        Monitors = new IMonitor[]
        {
            new Monitors.AirQualityMonitor(_db, _devices, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.AirQualityMonitor>()),
            new Monitors.PersonMonitor(_db, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.PersonMonitor>()),
            new Monitors.BatteryMonitor(_db, _devices, _rabbitMQ, _config, _loggerFactory.CreateLogger<Monitors.BatteryMonitor>()), 
            doors,
            ferien, 
            weather,
            // RuleMonitor has been moved to Sarah.Rules.WebApi as AlarmScheduleService and TemperatureScheduleService
        };

        await Task.WhenAll(Monitors.Select(m => m.Start()).ToArray());

        _logger.LogInformation("MonitoringService started, subscribing to NetworkEvents");

        await _rabbitMQ.SubscribeAsync<NetworkEventMessage<object>>("network.events", async (message) =>
        {
            // Convert the generic message to a more specific NetworkEvent type if possible
            // For now, create a basic NetworkEvent with the source node ID
            var networkEvent = new NetworkEvent<object>(message.SourceNodeId, message.Property, null);
            var networkEventSubscribers = Monitors.OfType<INetworkEventSubscriber>();
            foreach (var subscriber in networkEventSubscribers)
            {
                try
                {
                    await subscriber.Notify(networkEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying {SubscriberType} of network event", subscriber.GetType().Name);
                }
            }
        });

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
