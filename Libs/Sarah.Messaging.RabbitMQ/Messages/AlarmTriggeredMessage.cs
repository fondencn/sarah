namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a scheduled alarm fires.
/// Carries the serialized alarm content so downstream consumers can react accordingly.
/// </summary>
public class AlarmTriggeredMessage : AbstractMessage
{
    public long AlarmScheduleId { get; set; }

    public int ContentType { get; set; }

    public string? ContentJson { get; set; }

    public string? DisplayText { get; set; }

    public DateTime TriggeredAtUtc { get; set; }

    public bool IsSuppressedBySummer { get; set; }

    public AlarmTriggeredMessage()
    {
        Topic = MessageTopics.SchedulesAlarmTriggered;
    }
}