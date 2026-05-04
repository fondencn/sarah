using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules;

namespace Sarah.Rules.WebApi.Services;

/// <summary>
/// Background service that subscribes to RabbitMQ events and evaluates rules
/// </summary>
public class RulesEventSubscriber : BackgroundService
{
    private readonly RuleService _ruleService;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger<RulesEventSubscriber> _logger;

    public RulesEventSubscriber(
        RuleService ruleService,
        RabbitMQClient rabbitMQClient,
        ILogger<RulesEventSubscriber> logger)
    {
        _ruleService = ruleService;
        _rabbitMQClient = rabbitMQClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RulesEventSubscriber is starting");

        try
        {
            await _rabbitMQClient.ConnectAsync(stoppingToken);

            // Subscribe to specific network event types only

            await _rabbitMQClient.SubscribeAsync<ClickedEventMessage>(
                topic: "network.events.clicked",
                onMessage: HandleClickedEvent,
                cancellationToken: stoppingToken);

            await _rabbitMQClient.SubscribeAsync<TimerEventMessage>(
                topic: "network.events.timer",
                onMessage: HandleTimerEvent,
                cancellationToken: stoppingToken);

            await _rabbitMQClient.SubscribeAsync<PersonAvailabilityMessage>(
                topic: "person.availability",
                onMessage: HandlePersonAvailability,
                cancellationToken: stoppingToken);

            await _rabbitMQClient.SubscribeAsync<PersonGeoFenceMessage>(
                topic: "person.geofence",
                onMessage: HandlePersonGeoFence,
                cancellationToken: stoppingToken);

            await _rabbitMQClient.SubscribeAsync<AirQualityChangedMessage>(
                topic: "network.events.airquality",
                onMessage: HandleAirQualityChanged,
                cancellationToken: stoppingToken);

            _logger.LogInformation("RulesEventSubscriber subscribed to all event topics");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RulesEventSubscriber is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RulesEventSubscriber");
            throw;
        }
    }



    private async Task HandleClickedEvent(ClickedEventMessage message)
    {
        try
        {
            _logger.LogDebug("Received clicked event: Scene {SceneId} from node {NodeId}", 
                message.SceneId, message.SourceNodeId);

            var clickedEvent = new Sarah.API.BusinessObjects.ClickedEvent(
                message.SourceNodeId, 
                message.SceneId);
            await _ruleService.EvaluateRules(clickedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling clicked event");
        }
    }

    private async Task HandleTimerEvent(TimerEventMessage message)
    {
        try
        {
            _logger.LogDebug("Received timer event from node {NodeId}", message.SourceNodeId);

            var timerEvent = new Sarah.API.BusinessObjects.TimerEvent(message.SourceNodeId);
            await _ruleService.EvaluateRules(timerEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling timer event");
        }
    }

    private async Task HandlePersonAvailability(PersonAvailabilityMessage message)
    {
        try
        {
            _logger.LogDebug("Received person availability: {PersonName} is {Status}", 
                message.PersonName, message.IsAvailable ? "available" : "unavailable");

            var personEvent = new Sarah.API.BusinessObjects.PersonAvailabilityEvent(
                message.PersonId,
                message.PersonName,
                message.IsAvailable);
            await _ruleService.EvaluateRules(personEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling person availability event");
        }
    }

    private async Task HandlePersonGeoFence(PersonGeoFenceMessage message)
    {
        try
        {
            _logger.LogDebug("Received person geofence event: {PersonName}", message.PersonName);

            // Note: GeoFence objects would need to be resolved from IDs in a real implementation
            var geoFenceEvent = new Sarah.API.BusinessObjects.PersonGeoFenceEvent(
                message.PersonId,
                message.PersonName,
                null, // CurrentGeoFence - would need to resolve from message.CurrentGeoFenceId
                null); // PreviousGeoFence - would need to resolve from message.PreviousGeoFenceId
            await _ruleService.EvaluateRules(geoFenceEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling person geofence event");
        }
    }

    private async Task HandleAirQualityChanged(AirQualityChangedMessage message)
    {
        try
        {
            _logger.LogDebug("Received air quality changed: {Level} in {RoomName}", 
                message.Level, message.RoomName);

            var airQualityEvent = new Sarah.API.BusinessObjects.AirQualityChangedEvent(
                message.SourceNodeId,
                (Sarah.API.BusinessObjects.AirQualitityLevel)message.Level,
                message.Message,
                message.RoomName);
            await _ruleService.EvaluateRules(airQualityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling air quality changed event");
        }
    }

    public override void Dispose()
    {
        _rabbitMQClient?.Dispose();
        base.Dispose();
    }
}
