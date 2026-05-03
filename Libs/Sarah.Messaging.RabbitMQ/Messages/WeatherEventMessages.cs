using Sarah.Messaging.RabbitMQ;

namespace Sarah.Messaging.RabbitMQ.Messages;

public class WeatherWarningEventMessage : AbstractMessage
{
    /// <summary>
    /// Location name for which the warnings apply.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Preformatted speech output string built by WeatherMonitor.
    /// </summary>
    public string OutputString { get; set; } = string.Empty;

    /// <summary>
    /// Warning entries that should be announced.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Structured warning entries with additional DWD properties.
    /// </summary>
    public List<WeatherWarningDetailMessage> WarningDetails { get; set; } = new();

    public WeatherWarningEventMessage()
    {
        Topic = MessageTopics.WeatherWarning;
    }
}

/// <summary>
/// Structured weather warning entry used in WeatherWarningEventMessage.
/// </summary>
public class WeatherWarningDetailMessage
{
    public string Key { get; set; } = string.Empty;
    public string? RegionName { get; set; }
    public string? Description { get; set; }
    public string? Event { get; set; }
    public string? Headline { get; set; }
    public string? Instruction { get; set; }
    public int? Type { get; set; }
    public int? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDayWarning { get; set; }
    public string OutputString { get; set; } = string.Empty;
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
    /// Sunset time (null if not available)
    /// </summary>
    public DateTime? Sunset { get; set; }

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