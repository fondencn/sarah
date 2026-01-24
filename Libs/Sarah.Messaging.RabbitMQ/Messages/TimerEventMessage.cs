namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a timer event occurs
/// </summary>
public class TimerEventMessage : NetworkEventMessage
{
    public TimerEventMessage()
    {
        Topic = "network.events.timer";
        Property = "TimerTriggered";
    }

    public TimerEventMessage(byte sourceNodeId) : this()
    {
        SourceNodeId = sourceNodeId;
    }
}
