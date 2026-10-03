namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published by BatteryMonitor when one or more devices have a critically low battery.
/// Consumed by RuleService to generate voice output via a SayMessage.
/// </summary>
public class BatteryWarningMessage : AbstractMessage
{
    /// <summary>
    /// List of devices with critically low battery levels.
    /// </summary>
    public List<BatteryDeviceWarning> Warnings { get; set; } = new();

    public BatteryWarningMessage()
    {
        Topic = MessageTopics.MonitoringBatteryWarning;
    }
}

/// <summary>
/// Describes a single device with a critically low battery.
/// </summary>
public class BatteryDeviceWarning
{
    /// <summary>
    /// Human-readable device name (includes room name if known).
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Current battery level as a percentage (0–100).
    /// </summary>
    public float BatteryLevel { get; set; }
}
