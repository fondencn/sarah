using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Statistics.WebApi.Services;

/// <summary>Subscribes to energy measurement events and stores them as raw samples.</summary>
public class EnergyMeasurementConsumer : BackgroundService
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnergyMeasurementConsumer> _logger;

    public EnergyMeasurementConsumer(RabbitMQClient rabbitMQ, IServiceScopeFactory scopeFactory, ILogger<EnergyMeasurementConsumer> logger)
    {
        _rabbitMQ = rabbitMQ;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _rabbitMQ.SubscribeAsync<DeviceEnergyMeasuredMessage>(
                    MessageTopics.DeviceEnergyMeasured,
                    OnMessage,
                    cancellationToken: stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Subscribing to {Topic} failed, retrying in 10s", MessageTopics.DeviceEnergyMeasured);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task OnMessage(DeviceEnergyMeasuredMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<EnergySampleStore>();
        await store.StoreAsync(message);
    }
}
