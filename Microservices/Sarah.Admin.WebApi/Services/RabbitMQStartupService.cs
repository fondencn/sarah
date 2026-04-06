using Sarah.Messaging.RabbitMQ;

namespace Sarah.Admin;

/// <summary>
/// Connects RabbitMQ at startup so the channel is ready before the first publish.
/// </summary>
public class RabbitMQStartupService(RabbitMQClient rabbitMQClient, ILogger<RabbitMQStartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await rabbitMQClient.ConnectAsync(stoppingToken);
            logger.LogInformation("RabbitMQ connected");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to RabbitMQ at startup");
        }
    }
}
