using Microsoft.Extensions.Logging;
using Sarah.API.Extensions;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System.Collections.Generic;
using System.Linq;

namespace Sarah.Rules.Services;

/// <summary>
/// Handles weather warning messages and raises speech notifications
/// </summary>
public class WeatherWarningHandler : IHostedService, IDisposable
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly ILogger<WeatherWarningHandler> _logger;
    private readonly IConfiguration _config;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;

    public WeatherWarningHandler(RabbitMQClient rabbitMQ, ILogger<WeatherWarningHandler> logger, IConfiguration config)
    {
        _rabbitMQ = rabbitMQ ?? throw new ArgumentNullException(nameof(rabbitMQ));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
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

            var warnings = message.Warnings?
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct()
                .ToList()
                ?? new List<string>();

            if (warnings.Count == 0)
            {
                _logger.LogDebug("Weather warning message contained no warnings");
                return;
            }

            string locationPart = string.IsNullOrWhiteSpace(message.Location)
                ? string.Empty
                : $" fuer {message.Location}";

            // Raise a say message for the weather warning
            string warningMessage = $"Achtung, Wetterwarnung{locationPart}: {string.Join(". ", warnings)}";
            await _rabbitMQ.PublishAsync(new SayMessage(warningMessage));
            
            _logger.LogInformation("Published say message for weather warning ({WarningCount} items)", warnings.Count);
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            _disposed = true;
        }
    }
}
