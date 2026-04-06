namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a wall plug changes its on/off state or power metrics cross thresholds.
/// </summary>
public class WallPlugStateChangedMessage : NetworkEventMessage
{
    public bool IsOn { get; set; }
    public DateTime LastChangeToPowerLow { get; set; }
    public DateTime LastIncreasePower { get; set; }
    public DateTime LastDecreasePower { get; set; }

    public WallPlugStateChangedMessage()
    {
        Topic = MessageTopics.NetworkEventsWallPlugState;
        Property = "WallPlugState";
    }

    public WallPlugStateChangedMessage(byte sourceNodeId, bool isOn, DateTime lastChangeToPowerLow, DateTime lastIncreasePower, DateTime lastDecreasePower) : this()
    {
        SourceNodeId = sourceNodeId;
        IsOn = isOn;
        LastChangeToPowerLow = lastChangeToPowerLow;
        LastIncreasePower = lastIncreasePower;
        LastDecreasePower = lastDecreasePower;
    }
}
