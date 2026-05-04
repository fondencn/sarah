using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System.Threading.Channels;

namespace Sarah.SpeechServer;

public class SpeechEventSubscriber : BackgroundService
{
    private const int QueueCapacity = 50;

    private readonly string _hostName = Environment.MachineName;
    private readonly ISpeechService _speechService;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger<SpeechEventSubscriber> _logger;
    private readonly Channel<SayMessage> _sayQueue = Channel.CreateBounded<SayMessage>(
        new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

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
                topic: MessageTopics.SpeechSay,
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
                _speechService.SayWithVolume(message.Message, MapVolume(message.Volume));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued say message");
            }
        }
    }

    private static Sarah.API.BusinessObjects.SpeechVolume MapVolume(Sarah.Messaging.RabbitMQ.Messages.SpeechVolume volume) =>
        volume switch
        {
            Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Silent   => Sarah.API.BusinessObjects.SpeechVolume.Quieter,
            Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Quiet    => Sarah.API.BusinessObjects.SpeechVolume.Quieter,
            Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Normal   => Sarah.API.BusinessObjects.SpeechVolume.Normal,
            Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Loud     => Sarah.API.BusinessObjects.SpeechVolume.Louder,
            Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.VeryLoud => Sarah.API.BusinessObjects.SpeechVolume.VeryLoud,
            _                                                         => Sarah.API.BusinessObjects.SpeechVolume.Normal
        };

    private Task HandleSayMessage(SayMessage message)
    {
        if (!ShouldHandleMessageOnThisHost(message, _hostName))
        {
            _logger.LogDebug(
                "Skipping say message targeted to speaker '{TargetSpeaker}' on host '{HostName}'",
                message.TargetSpeaker,
                _hostName);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Received say message: {Message} for speaker: {Speaker}", 
            message.Message, 
            string.IsNullOrEmpty(message.TargetSpeaker) ? "all" : message.TargetSpeaker);

        if (!_sayQueue.Writer.TryWrite(message))
        {
            _logger.LogWarning("Speech queue is full (capacity {Capacity}); dropping message: {Message}", QueueCapacity, message.Message);
        }
        return Task.CompletedTask;
    }

    internal static bool ShouldHandleMessageOnThisHost(SayMessage message, string hostName)
    {
        if (message == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.TargetSpeaker))
        {
            return true;
        }

        return string.Equals(message.TargetSpeaker.Trim(), hostName, StringComparison.OrdinalIgnoreCase);
    }

    public override void Dispose()
    {
        _sayQueue.Writer.TryComplete();
        _rabbitMQClient?.Dispose();
        base.Dispose();
    }
}
