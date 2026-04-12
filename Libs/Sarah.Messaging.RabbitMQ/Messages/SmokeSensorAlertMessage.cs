namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a smoke sensor fires or clears its alarm.
/// </summary>
public class SmokeSensorAlertMessage : NetworkEventMessage
{
    /// <summary>True when the alarm is active (value > 0).</summary>
    public bool AlarmActive { get; set; }

    public SmokeSensorAlertMessage()
    {
        Topic = MessageTopics.NetworkEventsSmokeSensorAlert;
        Property = "SmokeSensorAlert";
    }

    public SmokeSensorAlertMessage(byte sourceNodeId, bool alarmActive) : this()
    {
        SourceNodeId = sourceNodeId;
        AlarmActive = alarmActive;
    }
}
