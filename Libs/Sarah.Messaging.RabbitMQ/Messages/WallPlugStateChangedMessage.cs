namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a wall plug changes its on/off state or power metrics cross thresholds.
/// </summary>
public class WallPlugEnabledChangedMessage : NetworkEventMessage
{
    public bool IsOn { get; set; }

    public WallPlugEnabledChangedMessage(bool isOn)
    {
        Topic = MessageTopics.NetworkEventsWallPlugEnabled;
        Property = "WallPlugEnabledState";
        IsOn = isOn;
    }
}

public class WallPlugPowerLowMessage : NetworkEventMessage
{
    public WallPlugPowerLowMessage()
    {
        Topic = MessageTopics.NetworkEventsWallPlugPowerLow;
        Property = "WallPlugPowerConsumptionChangedToLow";
    }
}
public class WallPlugPowerHighMessage : NetworkEventMessage
{
    public WallPlugPowerHighMessage()
    {
        Topic = MessageTopics.NetworkEventsWallPlugPowerHigh;
        Property = "WallPlugPowerConsumptionChangedToHigh";
    }
}
