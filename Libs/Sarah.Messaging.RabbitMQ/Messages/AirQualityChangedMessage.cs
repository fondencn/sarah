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
        Topic = MessageTopics.NetworkEventsAirQuality;
        Property = "AirQuality";
    }

    public AirQualityChangedMessage(byte SourceNodeId, AirQualityLevel Level, string Message, string RoomName) : this()
    {
        this.SourceNodeId = SourceNodeId;
        this.Level = Level;
        this.Message = Message;
        this.RoomName = RoomName;
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
