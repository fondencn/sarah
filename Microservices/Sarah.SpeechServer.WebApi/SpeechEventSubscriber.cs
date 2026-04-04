using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System.Threading.Channels;

namespace Sarah.SpeechServer;

public class SpeechEventSubscriber : BackgroundService
{
    private readonly ISpeechService _speechService;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger<SpeechEventSubscriber> _logger;
    private readonly Channel<SayMessage> _sayQueue = Channel.CreateUnbounded<SayMessage>(
        new UnboundedChannelOptions { SingleReader = true });

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

            // Process queued messages sequentially so sounds never play in parallel
            await ProcessQueueAsync(stoppingToken);
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

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (SayMessage message in _sayQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _speechService.SayWithVolume(message.Message, (Sarah.API.BusinessObjects.SpeechVolume)message.Volume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued say message");
            }
        }
    }

    private Task HandleSayMessage(SayMessage message)
    {
        _logger.LogInformation("Received say message: {Message} for speaker: {Speaker}", 
            message.Message, 
            string.IsNullOrEmpty(message.TargetSpeaker) ? "all" : message.TargetSpeaker);

        _sayQueue.Writer.TryWrite(message);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _sayQueue.Writer.TryComplete();
        _rabbitMQClient?.Dispose();
        base.Dispose();
    }
}
