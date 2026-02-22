namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a temperature schedule is changed (created, updated, or deleted).
/// Notifies subscribers that timer configuration needs to be recalculated.
/// </summary>
public class TemperatureScheduleChangedMessage : AbstractMessage
{
    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public enum ChangeType
    {
        Created,
        Updated,
        Deleted
    }

    /// <summary>
    /// The ID of the temperature schedule that changed
    /// </summary>
    public long TemperatureScheduleId { get; set; }

    /// <summary>
    /// The room ID associated with this schedule (if applicable)
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// The type of change that occurred
    /// </summary>
    public ChangeType Change { get; set; }

    public TemperatureScheduleChangedMessage()
    {
        Topic = MessageTopics.SchedulesTemperatureChanged;
    }
}
