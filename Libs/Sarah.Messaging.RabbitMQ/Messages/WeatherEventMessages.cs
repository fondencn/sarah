using Sarah.Messaging.RabbitMQ;

namespace Sarah.Messaging.RabbitMQ.Messages;

public class OutDoorTemperatureChangedEventMessage : AbstractMessage
{
    public OutDoorTemperatureChangedEventMessage(double newVal) 
    {
        Topic = MessageTopics.WeatherOutdoorTemperature;
        NewValue = newVal;
    }

    public double NewValue { get; set; }
}

public class WeatherWarningEventMessage : AbstractMessage
{
    public WeatherWarningEventMessage(string newVal) 
    {
        Topic = MessageTopics.WeatherWarning;
        NewValue = newVal;
    }

    public string NewValue { get; set; }
}

/// <summary>
/// Message published when weather forecast is updated
/// </summary>
public class WeatherForecastUpdatedMessage : AbstractMessage
{
    /// <summary>
    /// Current outdoor temperature in Celsius
    /// </summary>
    public double CurrentTemperature { get; set; }

    /// <summary>
    /// Average temperature for next 4 hours in Celsius (null if not available)
    /// </summary>
    public double? AverageTemperatureNext4Hours { get; set; }

    /// <summary>
    /// Sunrise time (null if not available)
    /// </summary>
    public DateTime? Sunrise { get; set; }

    /// <summary>
    /// Weather description for current conditions
    /// </summary>
    public string CurrentWeatherString { get; set; } = string.Empty;

    /// <summary>
    /// Weather forecast string for today
    /// </summary>
    public string ForecastStringForToday { get; set; } = string.Empty;

    /// <summary>
    /// Active weather warnings
    /// </summary>
    public string WeatherWarningString { get; set; } = string.Empty;

    /// <summary>
    /// Location name for the weather data
    /// </summary>
    public string Location { get; set; } = string.Empty;

    public WeatherForecastUpdatedMessage()
    {
        Topic = MessageTopics.WeatherForecastUpdated;
    }
}