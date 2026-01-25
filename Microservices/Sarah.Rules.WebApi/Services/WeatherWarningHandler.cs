using Microsoft.Extensions.Logging;
using Sarah.API.Extensions;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services;

/// <summary>
/// Handles weather warning messages and raises speech notifications
/// </summary>
public class WeatherWarningHandler : IHostedService
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly ILogger<WeatherWarningHandler> _logger;
    private readonly IConfiguration _config;
    private CancellationTokenSource? _cancellationTokenSource;

    public WeatherWarningHandler(RabbitMQClient rabbitMQ, ILogger<WeatherWarningHandler> logger, IConfiguration config)
    {
        _rabbitMQ = rabbitMQ;
        _logger = logger;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Subscribe to weather warning messages
        await _rabbitMQ.SubscribeAsync<WeatherWarningEventMessage>(
            topic: MessageTopics.WeatherWarning,
            onMessage: async msg =>
            {
                await HandleWeatherWarning(msg);
            },
            exchange: MessageTopics.WeatherWarning,
            cancellationToken: _cancellationTokenSource.Token);

        _logger.LogInformation("WeatherWarningHandler started and subscribed to weather warnings");
    }

    private async Task HandleWeatherWarning(WeatherWarningEventMessage message)
    {
        try
        {
            // Check if we're in silent hours
            if (DateTime.Now.IsInSilentTime(_config))
            {
                _logger.LogDebug("Weather warning received during silent hours, skipping speech notification");
                return;
            }

            // Raise a say message for the weather warning
            string warningMessage = $"Achtung, Wetterwarnung: {message.NewValue}";
            await _rabbitMQ.PublishAsync(new SayMessage(warningMessage));
            
            _logger.LogInformation("Published say message for weather warning: {Warning}", message.NewValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling weather warning message");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        _logger.LogInformation("WeatherWarningHandler stopped");
        return Task.CompletedTask;
    }
}
