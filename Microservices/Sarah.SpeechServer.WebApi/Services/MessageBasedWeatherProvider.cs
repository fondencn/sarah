using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.SpeechServer.Services;

/// <summary>
/// Local implementation of IWeatherProvider that subscribes to weather messages
/// and maintains state in memory for the SpeechServer microservice
/// </summary>
public class MessageBasedWeatherProvider : IWeatherProvider, IHostedService
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly ILogger<MessageBasedWeatherProvider> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    // Local state
    private double _currentOutdoorTemperature = 22.0;
    private double? _averageTemperatureNext4Hours;
    private DateTime? _sunrise;
    private string _currentWeatherString = string.Empty;
    private string _forecastStringForToday = string.Empty;
    private string _weatherWarningString = string.Empty;
    private Dictionary<DateTime, string> _forecastCache = new Dictionary<DateTime, string>();

    public MessageBasedWeatherProvider(RabbitMQClient rabbitMQ, ILogger<MessageBasedWeatherProvider> logger)
    {
        _rabbitMQ = rabbitMQ;
        _logger = logger;
    }

    public double CurrentOutdoorTemperature => _currentOutdoorTemperature;

    public double? AverageTemperatureNext4Hours => _averageTemperatureNext4Hours;

    public string GetCurrentWeatherString(bool addDebugOutput = false, bool getWarningDetails = false)
    {
        return _currentWeatherString;
    }

    public string GetWeatherForecastStringForToday()
    {
        return _forecastStringForToday;
    }

    public string GetWeatherForecastString(DateTime dteDate)
    {
        // Check cache for specific date forecast
        if (_forecastCache.TryGetValue(dteDate.Date, out string? forecast))
        {
            return forecast;
        }
        
        // If it's today, return today's forecast
        if (dteDate.Date == DateTime.Today)
        {
            return _forecastStringForToday;
        }
        
        return "Keine Wettervorhersage für diesen Tag verfügbar";
    }

    public string GetWeatherWarningString()
    {
        return _weatherWarningString;
    }

    public DateTime? GetSunrise()
    {
        return _sunrise;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        await _rabbitMQ.ConnectAsync(cancellationToken);
        
        // Subscribe to outdoor temperature changes
        await _rabbitMQ.SubscribeAsync<OutDoorTemperatureChangedEventMessage>(
            topic: MessageTopics.WeatherOutdoorTemperature,
            onMessage: async msg =>
            {
                _currentOutdoorTemperature = msg.NewValue;
                _logger.LogDebug("Updated outdoor temperature to {Temperature}°C", msg.NewValue);
                await Task.CompletedTask;
            },
            exchange: MessageTopics.WeatherOutdoorTemperature,
            cancellationToken: _cancellationTokenSource.Token);

        // Subscribe to weather forecast updates
        await _rabbitMQ.SubscribeAsync<WeatherForecastUpdatedMessage>(
            topic: MessageTopics.WeatherForecastUpdated,
            onMessage: async msg =>
            {
                _currentOutdoorTemperature = msg.CurrentTemperature;
                _averageTemperatureNext4Hours = msg.AverageTemperatureNext4Hours;
                _sunrise = msg.Sunrise;
                _currentWeatherString = msg.CurrentWeatherString;
                _forecastStringForToday = msg.ForecastStringForToday;
                _weatherWarningString = msg.WeatherWarningString;
                
                // Cache today's forecast
                _forecastCache[DateTime.Today] = msg.ForecastStringForToday;
                
                _logger.LogInformation("Updated weather forecast for {Location}", msg.Location);
                await Task.CompletedTask;
            },
            exchange: MessageTopics.WeatherForecastUpdated,
            cancellationToken: _cancellationTokenSource.Token);

        _logger.LogInformation("MessageBasedWeatherProvider started and subscribed to weather updates");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        _logger.LogInformation("MessageBasedWeatherProvider stopped");
        return Task.CompletedTask;
    }
}
