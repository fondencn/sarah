using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System.Text.Json;

namespace Sarah.DeviceService.WebApi.Services;

/// <summary>
/// RabbitMQ-based implementation of IEventProcessingService
/// </summary>
public class EventProcessingService : IEventProcessingService
{
    private readonly ILogger<EventProcessingService> _logger;
    private readonly RabbitMQClient _rabbitMQClient;
    private bool _isStarted = false;
    private readonly Dictionary<INetworkEventSubscriber, CancellationTokenSource> _networkSubscribers = new();
    private readonly Dictionary<ISpeechEventSubscriber, CancellationTokenSource> _speechSubscribers = new();

    public EventProcessingService(ILogger<EventProcessingService> logger, RabbitMQClient rabbitMQClient)
    {
        _logger = logger;
        _rabbitMQClient = rabbitMQClient;
    }

    public async Task Start()
    {
        if (_isStarted)
        {
            _logger.LogWarning("EventProcessingService already started");
            return;
        }

        try
        {
            await _rabbitMQClient.ConnectAsync();
            _isStarted = true;
            _logger.LogInformation("EventProcessingService started and connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start EventProcessingService");
            throw;
        }
    }

    public async Task SubscribeNetworkEventAsync(INetworkEventSubscriber subscriber, CancellationToken cancellationToken = default)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _networkSubscribers[subscriber] = cts;

            await _rabbitMQClient.SubscribeAsync<NetworkEventMessage<object>>(
                topic: "network.events.#",
                onMessage: async msg =>
                {
                    // Convert RabbitMQ message to NetworkEvent
                    var networkEvent = new NetworkEvent<object>(msg.SourceNodeId, msg.NewValue!, msg.Property);
                    await subscriber.Notify(networkEvent);
                },
                exchange: "network.events",
                cancellationToken: cts.Token);

            _logger.LogInformation("Subscribed network event subscriber to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe network events");
            throw;
        }
    }

    public async Task SubscribeSpeechEventAsync(ISpeechEventSubscriber subscriber, CancellationToken cancellationToken = default)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _speechSubscribers[subscriber] = cts;

            await _rabbitMQClient.SubscribeAsync<SayMessage>(
                topic: "speech.#",
                onMessage: async msg =>
                {
                    // Convert RabbitMQ message to SayEvent
                    var sayEvent = new SayEvent(msg.Message, msg.TargetSpeaker, (Sarah.API.BusinessObjects.SpeechVolume)msg.Volume);
                    await subscriber.Notify(sayEvent);
                },
                exchange: "speech.events",
                cancellationToken: cts.Token);

            _logger.LogInformation("Subscribed speech event subscriber to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe speech events");
            throw;
        }
    }

    public async Task PublishNetworkEventAsync<T>(NetworkEvent<T> networkEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new NetworkEventMessage<T>
            {
                SourceNodeId = networkEvent.SourceNodeId,
                Property = networkEvent.Property,
                NewValue = networkEvent.NewValue,
                Topic = $"network.events.{networkEvent.Property.ToLowerInvariant()}"
            };

            await _rabbitMQClient.PublishAsync(message, exchange: "network.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published network event - NodeId: {NodeId}, Property: {Property}", networkEvent.SourceNodeId, networkEvent.Property);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish network event - NodeId: {NodeId}, Property: {Property}", networkEvent.SourceNodeId, networkEvent.Property);
        }
    }

    public async Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new NetworkEventMessage<object>
            {
                SourceNodeId = networkEvent.SourceNodeId,
                Property = networkEvent.Property,
                NewValue = null!,
                Topic = $"network.events.{networkEvent.Property.ToLowerInvariant()}"
            };

            await _rabbitMQClient.PublishAsync(message, exchange: "network.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published network event - NodeId: {NodeId}, Property: {Property}", networkEvent.SourceNodeId, networkEvent.Property);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish network event - NodeId: {NodeId}, Property: {Property}", networkEvent.SourceNodeId, networkEvent.Property);
        }
    }

    public async Task PublishAirQualityEventAsync(AirQualityChangedEvent airQualityChangedEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var level = airQualityChangedEvent.Level switch
            {
                AirQualitityLevel.OK => Sarah.Messaging.RabbitMQ.Messages.AirQualityLevel.Excellent,
                AirQualitityLevel.Warning => Sarah.Messaging.RabbitMQ.Messages.AirQualityLevel.Fair,
                AirQualitityLevel.Bad => Sarah.Messaging.RabbitMQ.Messages.AirQualityLevel.Poor,
                AirQualitityLevel.SuperBad => Sarah.Messaging.RabbitMQ.Messages.AirQualityLevel.VeryPoor,
                _ => Sarah.Messaging.RabbitMQ.Messages.AirQualityLevel.Good
            };

            var message = new AirQualityChangedMessage(
                airQualityChangedEvent.SourceNodeId,
                level,
                airQualityChangedEvent.Message,
                airQualityChangedEvent.RoomName);

            await _rabbitMQClient.PublishAsync(message, exchange: "network.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published air quality event - Room: {Room}, Level: {Level}", airQualityChangedEvent.RoomName, level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish air quality event");
        }
    }

    public async Task PublishSay(SayEvent e, CancellationToken cancellationToken = default)
    {
        try
        {
            var volume = e.Volume switch
            {
                Sarah.API.BusinessObjects.SpeechVolume.Normal => Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Normal,
                Sarah.API.BusinessObjects.SpeechVolume.Louder => Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Loud,
                Sarah.API.BusinessObjects.SpeechVolume.VeryLoud => Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.VeryLoud,
                Sarah.API.BusinessObjects.SpeechVolume.Quieter => Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Quiet,
                _ => Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Normal
            };

            var message = new SayMessage(e.Message, e.TargetSpeaker, volume);
            await _rabbitMQClient.PublishAsync(message, exchange: "speech.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published say message - Speaker: {Speaker}", e.TargetSpeaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish say message");
        }
    }

    public async Task PublishPersonAvailabilityAsync(PersonAvailabilityEvent personAvailabilityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new PersonAvailabilityMessage(
                personAvailabilityEvent.Id_Person,
                personAvailabilityEvent.PersonName,
                personAvailabilityEvent.IsAvailable);

            await _rabbitMQClient.PublishAsync(message, exchange: "person.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published person availability - Person: {Person}, Available: {Available}", 
                personAvailabilityEvent.PersonName, personAvailabilityEvent.IsAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish person availability event");
        }
    }

    public async Task PublishGeoFenceEventAsync(PersonGeoFenceEvent personGeoFenceEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new PersonGeoFenceMessage(
                personGeoFenceEvent.Id_Person,
                personGeoFenceEvent.PersonName,
                personGeoFenceEvent.CurrentGeoFence,
                personGeoFenceEvent.PreviousGeoFence);

            await _rabbitMQClient.PublishAsync(message, exchange: "person.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published geofence event - Person: {Person}, Current: {Current}, Previous: {Previous}",
                personGeoFenceEvent.PersonName, personGeoFenceEvent.CurrentGeoFence, personGeoFenceEvent.PreviousGeoFence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish geofence event");
        }
    }

    public async Task PublishWeatherWarningEventAsync(WeatherWarningEvent weatherWarningEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new WeatherWarningEventMessage(weatherWarningEvent.NewValue);
            await _rabbitMQClient.PublishAsync(message, exchange: "weather.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published weather warning event - Warning: {Warning}", weatherWarningEvent.NewValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish weather warning event");
        }
    }

    public async Task PublishTimerEventAsync(TimerEvent timerEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new TimerEventMessage(timerEvent.SourceNodeId);
            await _rabbitMQClient.PublishAsync(message, exchange: "network.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published timer event - NodeId: {NodeId}", timerEvent.SourceNodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish timer event");
        }
    }

    public async Task PublishStartPlayAudioEventAsync(StartAudioEvent startAudioEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new StartAudioMessage(startAudioEvent.AudioFileName, startAudioEvent.TargetSpeaker);
            await _rabbitMQClient.PublishAsync(message, exchange: "speech.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published start audio event - File: {File}, Speaker: {Speaker}", 
                startAudioEvent.AudioFileName, startAudioEvent.TargetSpeaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish start audio event");
        }
    }

    public async Task PublishStopPlayAudioEventAsync(StopAudioEvent stopAudioEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new StopAudioMessage(stopAudioEvent.TargetSpeaker);
            await _rabbitMQClient.PublishAsync(message, exchange: "speech.events", cancellationToken: cancellationToken);
            _logger.LogDebug("Published stop audio event - Speaker: {Speaker}", stopAudioEvent.TargetSpeaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish stop audio event");
        }
    }
}
