namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a door or window sensor changes its open/closed state.
/// </summary>
public class DoorSensorStateChangedMessage : NetworkEventMessage
{
    /// <summary>
    /// True when the door/window is open; false when closed.
    /// </summary>
    public bool IsOpen { get; set; }

    public DoorSensorStateChangedMessage()
    {
        Topic = MessageTopics.NetworkEventsDoorState;
        Property = "DoorState";
    }

    public DoorSensorStateChangedMessage(byte sourceNodeId, bool isOpen) : this()
    {
        this.SourceNodeId = sourceNodeId;
        this.IsOpen = isOpen;
    }
}
