namespace Sarah.Messaging.RabbitMQ;

/// <summary>
/// Abstract base class for all messages in the system
/// </summary>
public abstract class AbstractMessage
{
    /// <summary>
    /// Unique identifier for this message
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The topic/routing key for this message
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Optional correlation ID for tracking related messages
    /// </summary>
    public string? CorrelationId { get; set; }
}
