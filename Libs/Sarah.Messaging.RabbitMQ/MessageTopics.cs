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
    /// Topic for door/window sensor open/close state changes.
    /// Published when a door or window sensor changes its open/closed state.
    /// </summary>
    public const string NetworkEventsDoorState = "network.events.doorstate";

    /// <summary>
    /// Topic for GPS tracker SOS button press events.
    /// Published when the button on a LoRaWAN GPS tracker is pressed or released.
    /// </summary>
    public const string NetworkEventsTrackerButton = "network.events.trackerbutton";

    /// <summary>
    /// Topic for wall plug state changes (on/off, power threshold crossings).
    /// </summary>
    public const string NetworkEventsWallPlugState = "network.events.wallplugstate";

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
    /// Topic for temperature schedule changes.
    /// Published when temperature schedules are created, updated, or deleted.
    /// Used to notify RuleService to reconfigure timer triggers.
    /// </summary>
    public const string SchedulesTemperatureChanged = "schedules.temperature.changed";

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
}
