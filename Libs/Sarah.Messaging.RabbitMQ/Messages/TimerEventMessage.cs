namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a timer event occurs
/// </summary>
public class TimerEventMessage : NetworkEventMessage
{
    public TimerEventMessage()
    {
        Topic = MessageTopics.NetworkEventsTimer;
        Property = "TimerTriggered";
    }

    public TimerEventMessage(byte SourceNodeId) : this()
    {
        this.SourceNodeId = SourceNodeId;
    }
}
