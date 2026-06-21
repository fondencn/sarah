using Sarah.API.BusinessObjects;
using Sarah.API.Extensions;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Rules.Conditions;
using System.Text;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;
using Sarah.Rules.Services.Kernel;
using Sarah.ServiceClients;

namespace Sarah.Rules
{
    /// <summary>
    /// Die RuleEngine (Singleton)
    /// </summary>
    public sealed class RuleService : BackgroundService, IRuleService, INetworkEventSubscriber, IDisposable
    {
        private readonly RabbitMQClient _rabbitMQ;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SmartHomeKernelService _smartHomeKernel;
        private readonly Sarah.Rules.Services.MessageBasedGridStateProvider _gridStateProvider;
        public IEnumerable<Rule> Rules => this.RuleStores.SelectMany(store => store.Rules);


        /// <summary>
        /// Verwaltung für Zeitgesteuerte Ereignisse
        /// </summary>
        private TimerEngine Timers { get; } 

        private readonly ILogger<RuleService> _logger;

        /// <summary>
        /// Alle Regel-Quellen
        /// </summary>
        private List<IRuleStore> RuleStores { get; } = new List<IRuleStore>();

        public RuleService(RabbitMQClient rabbitMQ, ILogger<RuleService> logger, IServiceScopeFactory scopeFactory, SmartHomeKernelService smartHomeKernel, Sarah.Rules.Services.MessageBasedGridStateProvider gridStateProvider) 
        { 
            this._rabbitMQ = rabbitMQ;
            this._logger = logger;
            this._scopeFactory = scopeFactory;
            this._smartHomeKernel = smartHomeKernel;
            this._gridStateProvider = gridStateProvider;
            this.Timers = new TimerEngine(rabbitMQ, logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RuleService is starting");

            try
            {
                await _rabbitMQ.ConnectAsync(stoppingToken);

                // Subscribe to specific network event types only
                await _rabbitMQ.SubscribeAsync<ClickedEventMessage>(
                    topic: MessageTopics.NetworkEventsClicked,
                    onMessage: HandleClickedEvent,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<TimerEventMessage>(
                    topic: MessageTopics.NetworkEventsTimer,
                    onMessage: HandleTimerEvent,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<PersonAvailabilityMessage>(
                    topic: MessageTopics.PersonAvailability,
                    onMessage: HandlePersonAvailability,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<PersonGeoFenceMessage>(
                    topic: MessageTopics.PersonGeoFence,
                    onMessage: HandlePersonGeoFence,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<AirQualityChangedMessage>(
                    topic: MessageTopics.NetworkEventsAirQuality,
                    onMessage: HandleAirQualityChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<TrackerButtonPressedMessage>(
                    topic: MessageTopics.NetworkEventsTrackerButton,
                    onMessage: HandleTrackerButtonPressed,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<WallPlugEnabledChangedMessage>(
                    topic: MessageTopics.NetworkEventsWallPlugEnabled,
                    onMessage: HandleWallPlugEnabledChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<WallPlugPowerLowMessage>(
                    topic: MessageTopics.NetworkEventsWallPlugPowerLow,
                    onMessage: HandleWallPlugPowerLow,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<WallPlugPowerHighMessage>(
                    topic: MessageTopics.NetworkEventsWallPlugPowerHigh,
                    onMessage: HandleWallPlugPowerHigh,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<MultiSensorStateChangedMessage>(
                    topic: MessageTopics.NetworkEventsMultiSensorState,
                    onMessage: HandleMultiSensorStateChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<SmokeSensorAlertMessage>(
                    topic: MessageTopics.NetworkEventsSmokeSensorAlert,
                    onMessage: HandleSmokeSensorAlert,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<AlarmScheduleChangedMessage>(
                    topic: MessageTopics.SchedulesAlarmChanged,
                    onMessage: HandleAlarmScheduleChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<AlarmTriggeredMessage>(
                    topic: MessageTopics.SchedulesAlarmTriggered,
                    onMessage: HandleAlarmTriggered,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<HolidayStatusChangedMessage>(
                    topic: MessageTopics.HolidaysStatusChanged,
                    onMessage: HandleHolidayStatusChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<WeatherWarningEventMessage>(
                    topic: MessageTopics.WeatherWarning,
                    onMessage: HandleWeatherWarning,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<WeatherForecastUpdatedMessage>(
                    topic: MessageTopics.WeatherForecastUpdated,
                    onMessage: HandleWeatherForecastUpdated,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<BatteryWarningMessage>(
                    topic: MessageTopics.MonitoringBatteryWarning,
                    onMessage: HandleBatteryWarning,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<DoorOrWindowOpenedMessage>(
                    topic: MessageTopics.NetworkEventsDoorOrWindowOpened,
                    onMessage: HandleDoorOrWindowOpened,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<DoorOrWindowStillOpenMessage>(
                    topic: MessageTopics.NetworkEventsDoorOrWindowStillOpen,
                    onMessage: HandleDoorOrWindowStillOpen,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<DoorOrWindowClosedMessage>(
                    topic: MessageTopics.NetworkEventsDoorOrWindowClosed,
                    onMessage: HandleDoorOrWindowClosed,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<GridStateChangedMessage>(
                    topic: MessageTopics.MonitoringGridStateChanged,
                    onMessage: HandleGridStateChanged,
                    cancellationToken: stoppingToken);

                await _rabbitMQ.SubscribeAsync<SpeechInputMessage>(
                    topic: MessageTopics.SpeechRecognized,
                    onMessage: HandleSpeechRecognized,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("RuleService subscribed to all event topics");

                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RuleService is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RuleService");
                throw;
            }
        }

        private async Task HandleClickedEvent(ClickedEventMessage message)
        {
            try
            {
                _logger.LogDebug("Received clicked event: Scene {SceneId} from node {NodeId}", 
                    message.SceneId, message.SourceNodeId);

                var clickedEvent = new ClickedEvent(message.SourceNodeId, message.SceneId);
                await EvaluateRules(clickedEvent);
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

                var timerEvent = new TimerEvent(message.SourceNodeId);
                await EvaluateRules(timerEvent);
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
                _logger.LogDebug("Received person availability: {PersonName} is {Available}", 
                    message.PersonName, message.IsAvailable ? "available" : "unavailable");

                var availabilityEvent = new PersonAvailabilityEvent(
                    message.PersonId, 
                    message.PersonName, 
                    message.IsAvailable);
                await EvaluateRules(availabilityEvent);
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
                _logger.LogDebug("Received geofence event: {PersonName}", message.PersonName);

                var geofenceEvent = new PersonGeoFenceEvent(
                    message.PersonId,
                    message.PersonName,
                    message.CurrentGeoFenceName, 
                    message.PreviousGeoFenceName); 
                await EvaluateRules(geofenceEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling geofence event");
            }
        }

        private async Task HandleAirQualityChanged(AirQualityChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received air quality changed event from node {NodeId}: {Level}", 
                    message.SourceNodeId, message.Level);

                var airQualityEvent = new AirQualityChangedEvent(
                    message.SourceNodeId,
                    (Sarah.API.BusinessObjects.AirQualitityLevel)message.Level,
                    message.Message ?? string.Empty,
                    message.RoomName ?? string.Empty);
                await EvaluateRules(airQualityEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling air quality changed event");
            }
        }

        private async Task HandleTrackerButtonPressed(TrackerButtonPressedMessage message)
        {
            try
            {
                _logger.LogDebug("Received tracker button event: node {NodeId}, isPressed={IsPressed}",
                    message.SourceNodeId, message.IsPressed);

                var trackerEvent = new TrackerButtonPressedEvent(message.SourceNodeId, message.IsPressed);
                await EvaluateRules(trackerEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tracker button pressed event");
            }
        }

        private async Task HandleWallPlugEnabledChanged(WallPlugEnabledChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received wall plug enabled changed event: node {NodeId}, isOn={IsOn}", message.SourceNodeId, message.IsOn);
                var evt = new WallPlugEnabledChangedEvent(message.SourceNodeId, message.IsOn);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling wall plug enabled changed event");
            }
        }

        private async Task HandleWallPlugPowerLow(WallPlugPowerLowMessage message)
        {
            try
            {
                _logger.LogDebug("Received wall plug power low event: node {NodeId}", message.SourceNodeId);
                var evt = new WallPlugPowerLowEvent(message.SourceNodeId);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling wall plug power low event");
            }
        }

        private async Task HandleWallPlugPowerHigh(WallPlugPowerHighMessage message)
        {
            try
            {
                _logger.LogDebug("Received wall plug power high event: node {NodeId}", message.SourceNodeId);
                var evt = new WallPlugPowerHighEvent(message.SourceNodeId);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling wall plug power high event");
            }
        }

        private async Task HandleMultiSensorStateChanged(MultiSensorStateChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received multi-sensor state changed event: node {NodeId}", message.SourceNodeId);
                var evt = new MultiSensorStateChangedEvent(message.SourceNodeId, message.Presence, message.Luminance);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling multi-sensor state changed event");
            }
        }

        private async Task HandleSmokeSensorAlert(SmokeSensorAlertMessage message)
        {
            try
            {
                _logger.LogDebug("Received smoke sensor alert event: node {NodeId}, alarmActive={AlarmActive}", message.SourceNodeId, message.AlarmActive);
                var evt = new SmokeSensorAlertEvent(message.SourceNodeId, message.AlarmActive);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling smoke sensor alert event");
            }
        }

        private async Task HandleAlarmScheduleChanged(AlarmScheduleChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received alarm schedule changed event: {AlarmScheduleId}, Change: {ChangeType}", 
                    message.AlarmScheduleId, message.Change);

                // Reconfigure timer rules to reflect schedule changes
                this.UpdateTimerRules();
                _logger.LogInformation("Timer rules reconfigured due to alarm schedule change");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling alarm schedule changed event");
            }
        }

        private async Task HandleAlarmTriggered(AlarmTriggeredMessage message)
        {
            try
            {
                _logger.LogInformation(
                    "Received alarm triggered event {AlarmScheduleId}: {DisplayText} (type {ContentType}, summerSuppressed={Suppressed})",
                    message.AlarmScheduleId,
                    message.DisplayText,
                    message.ContentType,
                    message.IsSuppressedBySummer);

                var evt = new AlarmTriggeredEvent(
                    message.AlarmScheduleId,
                    (int)message.ContentType,
                    message.ContentJson,
                    message.DisplayText ?? string.Empty,
                    message.IsSuppressedBySummer);

                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling alarm triggered event");
            }
        }

        private async Task HandleHolidayStatusChanged(HolidayStatusChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received holiday status changed event: {HolidayName}, Change: {ChangeType}", 
                    message.HolidayName, message.Change);

                // Activate/deactivate alarms based on holiday status
                // This is a placeholder - actual implementation would require access to AlarmScheduleService
                _logger.LogInformation("Holiday status changed: {HolidayName} - {ChangeType}", 
                    message.HolidayName, 
                    message.Change == HolidayStatusChangedMessage.ChangeType.Started ? "Started" : "Ended");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling holiday status changed event");
            }
        }

        private async Task HandleWeatherWarning(WeatherWarningEventMessage message)
        {
            try
            {
                _logger.LogDebug("Received weather warning event");
                var warnings = message.Warnings?
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .ToList()
                    ?? new List<string>();

                var warningDetails = message.WarningDetails?
                    .Select(d => new WeatherWarningDetail(
                        key: d.Key ?? string.Empty,
                        regionName: d.RegionName,
                        description: d.Description,
                        @event: d.Event,
                        headline: d.Headline,
                        instruction: d.Instruction,
                        type: d.Type,
                        level: d.Level,
                        startDate: d.StartDate,
                        endDate: d.EndDate,
                        isAllDayWarning: d.IsAllDayWarning,
                        outputString: d.OutputString ?? string.Empty))
                    .ToList()
                    ?? new List<WeatherWarningDetail>();

                var evt = new WeatherWarningEvent(
                    location: message.Location ?? string.Empty,
                    outputString: message.OutputString ?? string.Empty,
                    warnings: warnings,
                    warningDetails: warningDetails);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling weather warning event");
            }
        }

        private async Task HandleWeatherForecastUpdated(WeatherForecastUpdatedMessage message)
        {
            try
            {
                _logger.LogDebug("Received weather forecast updated event for {Location}", message.Location);
                var evt = new WeatherForecastUpdatedEvent(message.ForecastStringForToday ?? string.Empty);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling weather forecast updated event");
            }
        }

        private async Task HandleBatteryWarning(BatteryWarningMessage message)
        {
            try
            {
                _logger.LogDebug("Received battery warning event with {Count} warnings", message.Warnings.Count);
                var warnings = message.Warnings
                    .Select(w => new BatteryDeviceInfo(w.DeviceName, w.BatteryLevel))
                    .ToList();
                var evt = new BatteryWarningEvent(warnings);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling battery warning event");
            }
        }

        private async Task HandleDoorOrWindowOpened(DoorOrWindowOpenedMessage message)
        {
            try
            {
                _logger.LogDebug("Received door/window opened event: {DeviceName}, isWindow={IsWindow}",
                    message.DeviceName, message.IsWindow);
                var turnedOff = message.TurnedOffHeatings?
                    .Select(h => h.HeatingName)
                    .ToList() ?? new List<string>();
                var evt = new DoorOrWindowOpenedEvent(
                    message.SourceNodeId,
                    message.IsWindow,
                    message.DeviceName,
                    message.DeviceRoom,
                    turnedOff);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling door/window opened event");
            }
        }

        private async Task HandleDoorOrWindowStillOpen(DoorOrWindowStillOpenMessage message)
        {
            try
            {
                _logger.LogDebug("Received door/window still open event: {DeviceName}, openedSince={OpenedSince}",
                    message.DeviceName, message.OpenedSince);
                var evt = new DoorOrWindowStillOpenEvent(
                    message.SourceNodeId,
                    message.IsWindow,
                    message.DeviceName,
                    message.DeviceRoom,
                    message.OpenedSince);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling door/window still open event");
            }
        }

        private async Task HandleDoorOrWindowClosed(DoorOrWindowClosedMessage message)
        {
            try
            {
                _logger.LogDebug("Received door/window closed event: {DeviceName}, isWindow={IsWindow}",
                    message.DeviceName, message.IsWindow);
                var turnedOn = message.TurnedOnHeatings?
                    .Select(h => h.HeatingName + "|" + h.HeatingRoom)
                    .ToList() ?? new List<string>();
                var evt = new DoorOrWindowClosedEvent(
                    message.SourceNodeId,
                    message.IsWindow,
                    message.DeviceName,
                    message.DeviceRoom,
                    turnedOn,
                    message.OpenedDuration);
                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling door/window closed event");
            }
        }

        private async Task HandleGridStateChanged(GridStateChangedMessage message)
        {
            try
            {
                _logger.LogDebug("Received grid state changed event for zip {Zip}: {Previous} -> {Current}",
                    message.ZipCode, message.PreviousStateText ?? "unknown", message.CurrentStateText);

                _gridStateProvider.Update(message);

                var evt = new GridStateChangedEvent(
                    zip: message.ZipCode,
                    currentState: message.CurrentState,
                    currentStateText: message.CurrentStateText,
                    previousState: message.PreviousState,
                    previousStateText: message.PreviousStateText,
                    changedAtUtc: message.ChangedAtUtc);

                await EvaluateRules(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling grid state changed event");
            }
        }

        private async Task HandleSpeechRecognized(SpeechInputMessage message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message.RecognizedText))
                {
                    _logger.LogDebug("Ignoring empty speech input from host {HostName}", message.SpeakerHostName);
                    return;
                }

                _logger.LogInformation(
                    "Received recognized speech from host {HostName} at {Location}: {Text}",
                    message.SpeakerHostName,
                    message.SpeakerLocation,
                    message.RecognizedText);

                await _smartHomeKernel.ProcessChatMessageAsync(
                    message.RecognizedText,
                    message.SpeakerHostName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling recognized speech input");
            }
        }

        private object _evaluateRulesLock = new object();

        /// <summary>
        /// Per-node dedup tracker for broadcast rules (TargetNodeId == 0) triggered by sensor-specific events.
        /// Key: "{RuleName}|{SourceNodeId}", Value: last fire time.
        /// </summary>
        private readonly Dictionary<string, DateTime> _lastOccurrenceByRuleAndNode = new();

        /// <summary>
        /// Evaluiert alle Regeln führt bei zutreffen die verbundene Aktion aus
        /// </summary>
        public async Task EvaluateRules(NetworkEvent e)
        {
            string? deviceName = null;
            if (e.SourceNodeId > 0)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var deviceClient = scope.ServiceProvider.GetRequiredService<DeviceServiceClient>();
                    var device = await deviceClient.GetDeviceByNodeIdAsync(e.SourceNodeId);
                    deviceName = device?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve device name for node {NodeId}", e.SourceNodeId);
                }
            }

            try
            {
                await _smartHomeKernel.ProcessEventAsync(e, deviceName);
                await WriteExecutionLogAsync($"SemanticKernel:{e.GetType().Name}", success: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic Kernel error while processing event {EventType}", e.GetType().Name);
                await WriteExecutionLogAsync($"SemanticKernel:{e.GetType().Name}", success: false, errorMessage: ex.Message);
            }
        }

        private async Task WriteExecutionLogAsync(string ruleName, bool success, string? errorMessage = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.RuleExecutionLogs.Add(new RuleExecutionLogEntity
                {
                    RuleName = ruleName,
                    TriggeredAt = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write rule execution log for rule {RuleName}", ruleName);
            }
        }



        /// <summary>
        /// Registriert eine neue Regeldatenquelle für die Ruleengine
        /// </summary>
        /// <param name="storage"></param>
        public void RegisterRuleStore(IRuleStore storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }
            lock (this.RuleStores)
            {
                if (!this.RuleStores.Contains(storage))
                {
                    this.RuleStores.Add(storage);
                    this.UpdateTimerRules();
                    storage.Changed += (sender, e) => UpdateTimerRules();
                }
            }
        }

        /// <summary>
        /// Startet das Regelwerk unter beachtung der im Store vorhandenen Regeln
        /// </summary>
        /// <param name="store"></param>
        private void UpdateTimerRules()
        {
            this.Timers.Clear();
            foreach (var rule in Rules)
            {
                TimerCondition? timerCondition = rule.Condition as TimerCondition;
                if (timerCondition != null)
                {
                    if (timerCondition.IsOneShot)
                    {
                        if (timerCondition.DateTime.IsInFuture()) //nur Timer aktivieren, die mindestens 5 sekunden in der Zukunft liegen
                        {
                            this.Timers.Register(timerCondition.DateTime);
                            _logger.LogDebug("RecurrenceTimer for {RuleName} ticks at {TickTime} (one shot)", rule.Name, timerCondition.DateTime);
                        }
                    }
                    else
                    {
                        if (timerCondition.Recurrence != null)
                        {
                            this.Timers.Register(timerCondition.Recurrence);
                            _logger.LogDebug("RecurrenceTimer for {RuleName} ticks at {NextTick} (recurring)", rule.Name, timerCondition.Recurrence.GetNext());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notify-Methode des EventAggregators
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public async Task Notify(NetworkEvent e)
        {
            string? deviceName = null;
            if (e.SourceNodeId > 0)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var deviceClient = scope.ServiceProvider.GetRequiredService<DeviceServiceClient>();
                    var device = await deviceClient.GetDeviceByNodeIdAsync(e.SourceNodeId);
                    deviceName = device?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve device name for node {NodeId} in Notify", e.SourceNodeId);
                }
            }

            await _smartHomeKernel.ProcessEventAsync(e, deviceName);
        }

        /// <summary>
        /// From IDisposable
        /// </summary>
        public new void Dispose()
        {
            this.Timers?.Dispose();
            base.Dispose();
        }


        /// <summary>
        /// Verwaltung der Timer-Events für die einzelnen TimerConditions
        /// </summary>
        private sealed class TimerEngine : IDisposable
        {
            /// <summary>
            /// Alle registrierten Timer-Ereignisse
            /// </summary>
            private List<RecurrenceTimer> RegisteredRecurrences { get; } = new List<RecurrenceTimer>();
            private readonly ILogger _logger;
            private readonly RabbitMQClient _rabbitMQ;

            /// <summary>
            ///
            /// </summary>
            public TimerEngine(RabbitMQClient rabbitMQ, ILogger logger)
            {
                this._logger = logger;
                this._rabbitMQ = rabbitMQ;
                _RecreateTimersTimer = new Timer(RecreateTimer_Tick, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));
            }


            /// <summary>
            /// Timer, der jeden Tag überprüft, ob es noch Termine ohne Timer gibt weil diese bisher zu weit in der Zukunft lagen
            /// </summary>
            private readonly Timer _RecreateTimersTimer;

            private void RecreateTimer_Tick(object? state)
            {
                foreach (RecurrenceTimer item in this.RegisteredRecurrences.Where(timer => !timer.IsTimerCreated).ToList())
                {
                    item.CreateTimer();
                }
            }


            /// <summary>
            /// Löscht alle Timer-Einträge
            /// </summary>
            public void Clear()
            {
                RegisteredRecurrences.ForEach(timer => timer.Dispose());
                this.RegisteredRecurrences.Clear();
            }

            /// <summary>
            /// Registert eine neue Timer-Wiederholung bei der Engine
            /// </summary>
            /// <param name="recurrence"></param>
            internal void Register(TimerRecurrence recurrence)
            {
                if (recurrence.Until.HasValue && !recurrence.Until.Value.IsInFuture())
                {
                    _logger.LogDebug("recurrence.until ist in Vergangenheit -> übersprungen");
                }
                else
                {
                    RecurrenceTimer rt = new RecurrenceTimer(recurrence, _logger);
                    rt.Tick += recurrence_elapsed;
                    this.RegisteredRecurrences.Add(rt);
                }
            }

            internal void Register(DateTime occurance)
            {
                if (occurance.IsInFuture())
                {
                    RecurrenceTimer rt = new RecurrenceTimer(occurance, _logger);
                    rt.Tick += recurrence_elapsed;
                    this.RegisteredRecurrences.Add(rt);
                }
            }

            private async void recurrence_elapsed(object? sender, EventArgs e)
            {
                var message = new TimerEventMessage(0);
                await _rabbitMQ.PublishAsync(message);
            }

            /// <summary>
            /// From IDisposable
            /// </summary>
            public void Dispose()
            {
                this._RecreateTimersTimer?.Dispose();
            }



            /// <summary>
            /// Kapselt die Zeitsteuerung für eine Recurrence
            /// </summary>
            private sealed class RecurrenceTimer : IDisposable
            {
                private TimerRecurrence? RecurrenceDefinition { get; }
                private DateTime? OccuresOnceDate { get; }

                private Timer? _timer = null!;

                public bool IsTimerCreated => this._timer != null;
                private ILogger _logger = null!;
                public event EventHandler? Tick = null!;

                public RecurrenceTimer(TimerRecurrence recurrenceDefinition, ILogger logger)
                {
                    this.RecurrenceDefinition = recurrenceDefinition;
                    this.OccuresOnceDate = null;
                    this._logger = logger;
                    this.CreateTimer();
                }

                public RecurrenceTimer(DateTime occurance, ILogger logger)
                {
                    this.RecurrenceDefinition = null;
                    this.OccuresOnceDate = occurance;
                    this._logger = logger;
                    this.CreateTimer();
                }

                /// <summary>
                /// Erstellt einen Betriebssystemtimer für entweder einen festen Termin oder eine Terminserie.
                /// Falls das nächste Ereignis in der Vergangenheit oder zu weit in der Zukunft liegt,
                /// wird kein Betriebssystemtimer erstellt und IsTimerCreated bleibt false.
                /// Diese Methode muss dann später wieder aufgerufen werden, um den Timer zu erstellen.
                /// </summary>
                public void CreateTimer()
                {
                    if (this.RecurrenceDefinition != null)
                    {
                        DateTime nextOccurence = this.RecurrenceDefinition.GetNext();
                        this._timer = CreateOneShotTimer(nextOccurence);
                    }
                    else if (this.OccuresOnceDate.HasValue)
                    {
                        this._timer = CreateOneShotTimer(this.OccuresOnceDate.Value);
                    }
                    else
                    {
                        throw new InvalidOperationException("Weder Recurrence noch DateTime angegeben, kann keinen Timer für diesen Temrin erstellen");
                    }
                }

                private Timer? CreateOneShotTimer(DateTime nextOccurence)
                {
                    TimeSpan dueTime = (nextOccurence - DateTime.Now);
                    if (dueTime.TotalSeconds > 0 && dueTime.TotalMilliseconds < (Int32.MaxValue - 2))
                    {
                        return new Timer(timer_tick_internal, null, dueTime, TimeSpan.FromMilliseconds(0));
                    }
                    else if (dueTime.TotalMilliseconds >= (Int32.MaxValue - 2))
                    {
                        _logger.LogWarning("Timer zu weit in der Zukunft -> wird nicht gestartet");
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("Timer läge in der Vergangenheit -> wird nicht gestartet");
                        return null;
                    }
                }

                private void timer_tick_internal(object? state)
                {
                    this.Tick?.Invoke(this, EventArgs.Empty);
                    this._timer?.Dispose();
                    if (this.RecurrenceDefinition != null)
                    {
                        DateTime next = this.RecurrenceDefinition.GetNext();
                        if (this.RecurrenceDefinition.Until.HasValue && this.RecurrenceDefinition.Until.Value < next)
                        {
                            _logger.LogDebug("Recurrence abgebrochen, da Unil in Vergangenheit liegt");
                        }
                        else
                        {
                            this._timer = CreateOneShotTimer(next);
                        }
                    } // else: OneShot Timer.
                }

                public void Dispose()
                {
                    this._timer?.Dispose();
                }
            }

        }
    }
}
