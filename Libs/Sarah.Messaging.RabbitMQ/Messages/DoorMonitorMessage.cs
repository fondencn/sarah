namespace Sarah.Messaging.RabbitMQ.Messages;


public abstract class DoorOrWindowMessage : NetworkEventMessage
{
    public bool IsDoor { get; set; }
    public bool IsOpened { get; set; }
    public bool IsWindow {get;set;}
    public string DeviceName {get;set;} = string.Empty;
    public string DeviceRoom {get;set;} = string.Empty;
}

public class DoorOrWindowOpenedMessage : DoorOrWindowMessage
{
    public TurnedOffHeatingInfo[]? TurnedOffHeatings { get; set; }
    public DoorOrWindowOpenedMessage()
    {
        Topic = MessageTopics.NetworkEventsDoorOrWindowOpened;
        Property = "IsDoorOrWindowOpened";
    }
}

public class DoorOrWindowStillOpenMessage : DoorOrWindowMessage
{
    public TimeSpan OpenedSince { get; set; }

    public DoorOrWindowStillOpenMessage()
    {
        Topic = MessageTopics.NetworkEventsDoorOrWindowStillOpen;
        Property = "DoorOrWindowStillOpenReport";
    }
}


public class DoorOrWindowClosedMessage : DoorOrWindowMessage
{
    public TurnedOnHeatingInfo[]? TurnedOnHeatings { get; set; }
    public TimeSpan OpenedDuration { get; set; }
    public DoorOrWindowClosedMessage()
    {
        Topic = MessageTopics.NetworkEventsDoorOrWindowClosed;
        Property = "IsDoorOrWindowClosed";
    }
}

public class TurnedOffHeatingInfo
{
    public byte HeatingNodeId { get; set; }
    public string HeatingName { get; set; } = string.Empty;
    public string HeatingRoom { get; set; } = string.Empty;
}


public class TurnedOnHeatingInfo
{
    public byte HeatingNodeId { get; set; }
    public string HeatingName { get; set; } = string.Empty;
    public string HeatingRoom { get; set; } = string.Empty;
}