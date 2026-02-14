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

    // Weather-related topics
    
    /// <summary>
    /// Topic for outdoor temperature change events.
    /// Published when external temperature sensors report new values.
    /// </summary>
    public const string WeatherOutdoorTemperature = "weather.outdoortemperature";
    
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
}
