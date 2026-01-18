using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.SpeechServer;

public class SpeechEventSubscriber : BackgroundService
{
    private readonly ISpeechService _speechService;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger<SpeechEventSubscriber> _logger;

    public SpeechEventSubscriber(
        ISpeechService speechService, 
        RabbitMQClient rabbitMQClient,
        ILogger<SpeechEventSubscriber> logger)
    {
        _speechService = speechService;
        _rabbitMQClient = rabbitMQClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SpeechEventSubscriber starting...");

        try
        {
            // Connect to RabbitMQ
            await _rabbitMQClient.ConnectAsync(stoppingToken);

            // Subscribe to SayMessage events
            await _rabbitMQClient.SubscribeAsync<SayMessage>(
                topic: "speech.say",
                onMessage: HandleSayMessage,
                cancellationToken: stoppingToken);

            _logger.LogInformation("SpeechEventSubscriber subscribed to speech.say messages");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SpeechEventSubscriber stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SpeechEventSubscriber");
            throw;
        }
    }

    private async Task HandleSayMessage(SayMessage message)
    {
        try
        {
            _logger.LogInformation("Received say message: {Message} for speaker: {Speaker}", 
                message.Message, 
                string.IsNullOrEmpty(message.TargetSpeaker) ? "all" : message.TargetSpeaker);

            _speechService.SayWithVolume(message.Message, (Sarah.API.BusinessObjects.SpeechVolume)message.Volume);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling say message");
        }

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _rabbitMQClient?.Dispose();
        base.Dispose();
    }
}
