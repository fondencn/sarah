namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Base class for network event messages from ZWave network
/// </summary>
public abstract class NetworkEventMessage : AbstractMessage
{
    /// <summary>
    /// ZWave Node ID of the device that triggered the event
    /// </summary>
    public byte SourceNodeId { get; set; }

    /// <summary>
    /// Name of the changed property
    /// </summary>
    public string Property { get; set; } = string.Empty;

    protected NetworkEventMessage()
    {
        Topic = MessageTopics.NetworkEvents;
    }
}

/// <summary>
/// Generic network event message with a typed value
/// </summary>
/// <typeparam name="TValue">Type of the value</typeparam>
public class NetworkEventMessage<TValue> : NetworkEventMessage
{
    /// <summary>
    /// The new value
    /// </summary>
    public TValue? NewValue { get; set; }
}
