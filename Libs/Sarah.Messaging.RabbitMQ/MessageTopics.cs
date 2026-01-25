namespace Sarah.Messaging.RabbitMQ;

/// <summary>
/// Central definition of all message topics used in the system
/// </summary>
public static class MessageTopics
{
    // Speech-related topics
    public const string SpeechSay = "speech.say";
    public const string SpeechAudioStart = "speech.audio.start";
    public const string SpeechAudioStop = "speech.audio.stop";

    // Network event topics
    public const string NetworkEvents = "network.events";
    public const string NetworkEventsTimer = "network.events.timer";
    public const string NetworkEventsClicked = "network.events.clicked";
    public const string NetworkEventsAirQuality = "network.events.airquality";

    // Weather-related topics
    public const string WeatherOutdoorTemperature = "weather.outdoortemperature";
    public const string WeatherWarning = "weather.warning";
    public const string WeatherForecastUpdated = "weather.forecast.updated";

    // Person-related topics
    public const string PersonAvailability = "person.availability";
    public const string PersonGeoFence = "person.geofence";
}
