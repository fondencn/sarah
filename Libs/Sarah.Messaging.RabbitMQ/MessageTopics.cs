namespace Sarah.Messaging.RabbitMQ;

/// <summary>
/// Central definition of all message topics used in the system.
/// Topics are used as routing keys for RabbitMQ message distribution.
/// </summary>
public static class MessageTopics
{
    // Speech-related topics
    
    /// <summary>
    /// Topic for text-to-speech messages requesting voice output on speakers.
    /// Used to trigger speech synthesis across the system.
    /// </summary>
    public const string SpeechSay = "speech.say";
    
    /// <summary>
    /// Topic for starting audio playback on speakers.
    /// Used to initiate audio file playback on target devices.
    /// </summary>
    public const string SpeechAudioStart = "speech.audio.start";
    
    /// <summary>
    /// Topic for stopping audio playback on speakers.
    /// Used to halt ongoing audio playback on target devices.
    /// </summary>
    public const string SpeechAudioStop = "speech.audio.stop";

    /// <summary>
    /// Topic for recognized speech input emitted by SpeechServer.
    /// Contains transcribed user text plus source speaker host metadata.
    /// </summary>
    public const string SpeechRecognized = "speech.recognized";

    // Network event topics
    
    /// <summary>
    /// Base topic for general network events from ZWave devices.
    /// Used for broadcasting device state changes in the network.
    /// </summary>
    public const string NetworkEvents = "network.events";
    
    /// <summary>
    /// Topic for timer-triggered events from network devices.
    /// Used when devices report scheduled or timer-based activations.
    /// </summary>
    public const string NetworkEventsTimer = "network.events.timer";
    
    /// <summary>
    /// Topic for button click events from network devices.
    /// Used when hardware buttons or switches are pressed.
    /// </summary>
    public const string NetworkEventsClicked = "network.events.clicked";
    
    /// <summary>
    /// Topic for air quality sensor events.
    /// Used to broadcast changes in room air quality measurements.
    /// </summary>
    public const string NetworkEventsAirQuality = "network.events.airquality";

    /// <summary>
    /// Topic for GPS tracker SOS button press events.
    /// Published when the button on a LoRaWAN GPS tracker is pressed or released.
    /// </summary>
    public const string NetworkEventsTrackerButton = "network.events.trackerbutton";

    /// <summary>
    /// Topic for wall plug enabled state changes (on/off).
    /// </summary>
    public const string NetworkEventsWallPlugEnabled = "network.events.wallplugenabled";

    /// <summary>
    /// Topic for multi-sensor presence/luminance state changes.
    /// </summary>
    public const string NetworkEventsMultiSensorState = "network.events.multisensorstate";

    /// <summary>
    /// Topic for smoke sensor alarm state changes.
    /// </summary>
    public const string NetworkEventsSmokeSensorAlert = "network.events.smokesensoralert";

    // Weather-related topics
    
    /// <summary>
    /// Topic for weather warning notifications.
    /// Published when new weather warnings are issued or periodic re-announcements occur.
    /// </summary>
    public const string WeatherWarning = "weather.warning";
    
    /// <summary>
    /// Topic for comprehensive weather forecast updates.
    /// Published when weather forecast data is refreshed, includes current conditions,
    /// forecast, warnings, and sunrise times for cross-microservice consumption.
    /// </summary>
    public const string WeatherForecastUpdated = "weather.forecast.updated";

    // Person-related topics
    
    /// <summary>
    /// Topic for person availability (home presence) status changes.
    /// Published when a person's presence status changes (arrives home or leaves).
    /// </summary>
    public const string PersonAvailability = "person.availability";
    
    /// <summary>
    /// Topic for geofence boundary crossing events.
    /// Published when a person enters or exits a defined geographical zone.
    /// </summary>
    public const string PersonGeoFence = "person.geofence";

    // Schedule-related topics

    /// <summary>
    /// Topic for alarm schedule changes.
    /// Published when alarm schedules are created, updated, or deleted.
    /// Used to notify RuleService to reconfigure timer triggers.
    /// </summary>
    public const string SchedulesAlarmChanged = "schedules.alarm.changed";

    /// <summary>
    /// Topic for fired alarm schedules.
    /// Published when a scheduled alarm reaches its trigger time.
    /// </summary>
    public const string SchedulesAlarmTriggered = "schedules.alarm.triggered";

    // Holiday-related topics

    /// <summary>
    /// Topic for holiday status changes.
    /// Published when holidays start or end.
    /// Used to notify RuleService to activate/deactivate holiday-dependent alarms.
    /// </summary>
    public const string HolidaysStatusChanged = "holidays.status.changed";

    // Monitoring alert topics

    /// <summary>
    /// Topic for battery warning alerts from the BatteryMonitor.
    /// Published when one or more devices report a critically low battery level.
    /// Consumed by RuleService to generate voice output.
    /// </summary>
    public const string MonitoringBatteryWarning = "monitoring.battery.warning";

    /// <summary>
    /// Topic for door/window monitoring alerts from the DoorMonitor.
    /// Published when a door or window is opened, still open after a threshold, or closed.
    /// Consumed by RuleService to generate voice output.
    /// </summary>
    public const string MonitoringDoorAlert = "monitoring.door.alert";

    /// <summary>
    /// Topic for power grid state changes from the GridStateMonitor.
    /// Published when the current StromGedacht grid stage changes.
    /// </summary>
    public const string MonitoringGridStateChanged = "monitoring.grid.state.changed";

    /// <summary>
    /// Topic for wall plug power consumption crossing low threshold.
    /// Published when a wall plug's power consumption drops below the defined low threshold.
    /// </summary>
    public const string NetworkEventsWallPlugPowerLow = "network.events.wallplug.power.low";

    /// <summary>
    /// Topic for wall plug power consumption crossing high threshold.
    /// Published when a wall plug's power consumption rises above the defined high threshold.
    /// </summary>
    public const string NetworkEventsWallPlugPowerHigh = "network.events.wallplug.power.high";

    /// <summary>
    /// Topic for door or window closed events from the DoorMonitor.
    /// Published when a door or window is closed, includes info on turned-on heatings.
    /// </summary>
    public const string NetworkEventsDoorOrWindowClosed = "network.events.doororwindow.closed";

    /// <summary>
    /// Topic for door or window still open events from the DoorMonitor.
    /// Published when a door or window remains open for some time; includes opened duration.
    /// </summary>
    public const string NetworkEventsDoorOrWindowStillOpen = "network.events.doororwindow.stillopen";

    /// <summary>
    /// Topic for door or window opened events from the DoorMonitor.
    /// Published when a door or window is opened, includes info on turned-off heatings.
    /// </summary>
    public const string NetworkEventsDoorOrWindowOpened = "network.events.doororwindow.opened";
}
