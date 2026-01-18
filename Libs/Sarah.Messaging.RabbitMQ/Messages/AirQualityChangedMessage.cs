namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when air quality changes in a room
/// </summary>
public class AirQualityChangedMessage : NetworkEventMessage
{
    /// <summary>
    /// Air quality level
    /// </summary>
    public AirQualityLevel Level { get; set; }

    /// <summary>
    /// Descriptive message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Name of the room
    /// </summary>
    public string RoomName { get; set; } = string.Empty;

    public AirQualityChangedMessage()
    {
        Topic = "network.events.airquality";
        Property = "AirQuality";
    }

    public AirQualityChangedMessage(byte sourceNodeId, AirQualityLevel level, string message, string roomName) : this()
    {
        SourceNodeId = sourceNodeId;
        Level = level;
        Message = message;
        RoomName = roomName;
    }
}

/// <summary>
/// Air quality levels
/// </summary>
public enum AirQualityLevel
{
    Excellent = 0,
    Good = 1,
    Fair = 2,
    Poor = 3,
    VeryPoor = 4
}
