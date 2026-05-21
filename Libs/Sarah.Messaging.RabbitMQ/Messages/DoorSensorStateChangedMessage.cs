namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a door or window sensor changes its open/closed state.
/// </summary>
public class DoorSensorStateChangedMessage : NetworkEventMessage
{
    /// <summary>
    /// New sensor state after the transition.
    /// </summary>
    public DoorSensorStateValue State { get; set; }

    /// <summary>
    /// UTC timestamp when the state transition happened on the producer side.
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }

    /// <summary>
    /// Convenience accessor for consumers that only need open/closed as bool.
    /// </summary>
    public bool IsOpen => State == DoorSensorStateValue.Open;

    public DoorSensorStateChangedMessage()
    {
        Topic = MessageTopics.NetworkEventsDoorState;
        Property = "DoorState";
    }

    public DoorSensorStateChangedMessage(byte sourceNodeId, DoorSensorStateValue state, DateTime changedAtUtc) : this()
    {
        this.SourceNodeId = sourceNodeId;
        this.State = state;
        this.ChangedAtUtc = changedAtUtc;
    }
}

/// <summary>
/// Canonical door/window state used for typed sensor transition messages.
/// </summary>
public enum DoorSensorStateValue
{
    Closed = 0,
    Open = 1
}
