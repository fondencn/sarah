namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a device reports a power / energy measurement.
/// </summary>
public class DeviceEnergyMeasuredMessage : AbstractMessage
{
    /// <summary>Node ID of the reporting device.</summary>
    public int DeviceId { get; set; }

    /// <summary>Display name of the device at measurement time.</summary>
    public string? DeviceName { get; set; }

    /// <summary>Time of the measurement (UTC).</summary>
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Current power in watts.</summary>
    public double PowerW { get; set; }

    /// <summary>Optional cumulative meter reading in kWh.</summary>
    public double? EnergyKwhTotal { get; set; }

    /// <summary>Unit of <see cref="PowerW"/>.</summary>
    public string Unit { get; set; } = "W";

    public DeviceEnergyMeasuredMessage()
    {
        Topic = MessageTopics.DeviceEnergyMeasured;
    }
}
