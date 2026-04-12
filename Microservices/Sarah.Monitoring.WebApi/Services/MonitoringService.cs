using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Monitoring.Monitors;

namespace Sarah.Monitoring;

public class MonitoringService (IPersonService _personService, DeviceServiceClient _deviceServiceClient, RoomServiceClient _roomServiceClient, RabbitMQClient _rabbitMQ, IConfiguration _config, ILoggerFactory _loggerFactory, ILogger<MonitoringService> _logger, IHttpClientFactory _httpClientFactory) : BackgroundService
{
    public IWeatherProvider Weather  => this.Monitors.OfType<IWeatherProvider>().FirstOrDefault() ?? throw new InvalidOperationException("No IWeatherProvider monitor available");

    public FerienMonitor Ferien => this.Monitors.OfType<FerienMonitor>().FirstOrDefault() ?? throw new InvalidOperationException("No IFerienInfoProvider monitor available");

    private  IMonitor[] Monitors {get; set;} = Array.Empty<IMonitor>();

    public Task Start()
    {
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var roomSnapshot = await _roomServiceClient.GetAllRoomsAsync();

        var weather = new Monitors.WeatherMonitor(_config, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.WeatherMonitor>(), _httpClientFactory);
        weather.WarnLocation = _config["WeatherWarnLocation"] ?? "Berlin";
        var ferien = new Monitors.FerienMonitor(_config, _loggerFactory.CreateLogger<Monitors.FerienMonitor>(), _rabbitMQ);
        var doors = new Monitors.DoorMonitor(roomSnapshot, weather, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.DoorMonitor>(), _deviceServiceClient);
        Monitors = new IMonitor[]
        {
            new Monitors.AirQualityMonitor(roomSnapshot, _rabbitMQ, _config, _loggerFactory.CreateLogger<Monitors.AirQualityMonitor>(), _deviceServiceClient),
            new Monitors.PersonMonitor(_personService, _rabbitMQ, _loggerFactory.CreateLogger<Monitors.PersonMonitor>()),
            new Monitors.BatteryMonitor(roomSnapshot, _deviceServiceClient, _rabbitMQ, _config, _loggerFactory.CreateLogger<Monitors.BatteryMonitor>()),
            doors,
            ferien,
            weather,
        };

        await Task.WhenAll(Monitors.Select(m => m.Start()).ToArray());

        _logger.LogInformation("MonitoringService started, subscribing to NetworkEvents");

        await _rabbitMQ.SubscribeAsync<NetworkEventMessage<object>>("network.events", async (message) =>
        {
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

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

