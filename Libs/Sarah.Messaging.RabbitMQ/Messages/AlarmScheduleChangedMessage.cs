namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when an alarm schedule is changed (created, updated, or deleted).
/// Notifies subscribers that timer configuration needs to be recalculated.
/// </summary>
public class AlarmScheduleChangedMessage : AbstractMessage
{
    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public enum ChangeType
    {
        Created,
        Updated,
        Deleted,
        ActivationStateChanged
    }

    /// <summary>
    /// The ID of the alarm schedule that changed
    /// </summary>
    public long AlarmScheduleId { get; set; }

    /// <summary>
    /// The type of change that occurred
    /// </summary>
    public ChangeType Change { get; set; }

    public AlarmScheduleChangedMessage()
    {
        Topic = MessageTopics.SchedulesAlarmChanged;
    }
}
