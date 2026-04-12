using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services;

/// <summary>
/// Local implementation of IWeatherProvider that subscribes to weather messages
/// and maintains state in memory for the Rules microservice
/// </summary>
public class MessageBasedWeatherProvider : IWeatherProvider, IHostedService
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly ILogger<MessageBasedWeatherProvider> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _stateLock = new object();

    // Local state
    private double _currentOutdoorTemperature = 22.0;
    private double? _averageTemperatureNext4Hours;
    private DateTime? _sunrise;
    private string _currentWeatherString = string.Empty;
    private string _forecastStringForToday = string.Empty;
    private string _weatherWarningString = string.Empty;

    public MessageBasedWeatherProvider(RabbitMQClient rabbitMQ, ILogger<MessageBasedWeatherProvider> logger)
    {
        _rabbitMQ = rabbitMQ;
        _logger = logger;
    }

    public double CurrentOutdoorTemperature
    {
        get
        {
            lock (_stateLock)
            {
                return _currentOutdoorTemperature;
            }
        }
    }

    public double? AverageTemperatureNext4Hours
    {
        get
        {
            lock (_stateLock)
            {
                return _averageTemperatureNext4Hours;
            }
        }
    }

    public string GetCurrentWeatherString(bool addDebugOutput = false, bool getWarningDetails = false)
    {
        lock (_stateLock)
        {
            return _currentWeatherString;
        }
    }

    public string GetWeatherForecastStringForToday()
    {
        lock (_stateLock)
        {
            return _forecastStringForToday;
        }
    }

    public string GetWeatherForecastString(DateTime dteDate)
    {
        lock (_stateLock)
        {
            // For specific dates, return a generic message as we only cache today's forecast
            if (dteDate.Date == DateTime.Today)
            {
                return _forecastStringForToday;
            }
            return "Keine Wettervorhersage für diesen Tag verfügbar";
        }
    }

    public string GetWeatherWarningString()
    {
        lock (_stateLock)
        {
            return _weatherWarningString;
        }
    }

    public DateTime? GetSunrise()
    {
        lock (_stateLock)
        {
            return _sunrise;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        await _rabbitMQ.ConnectAsync(cancellationToken);
        
        // Subscribe to weather forecast updates
        await _rabbitMQ.SubscribeAsync<WeatherForecastUpdatedMessage>(
            topic: MessageTopics.WeatherForecastUpdated,
            onMessage: async msg =>
            {
                lock (_stateLock)
                {
                    _currentOutdoorTemperature = msg.CurrentTemperature;
                    _averageTemperatureNext4Hours = msg.AverageTemperatureNext4Hours;
                    _sunrise = msg.Sunrise;
                    _currentWeatherString = msg.CurrentWeatherString;
                    _forecastStringForToday = msg.ForecastStringForToday;
                    _weatherWarningString = msg.WeatherWarningString;
                }
                
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
