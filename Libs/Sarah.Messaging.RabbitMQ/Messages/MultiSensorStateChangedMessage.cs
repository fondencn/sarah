namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a multi-sensor (Fibaro Eye etc.) changes presence or luminance.
/// </summary>
public class MultiSensorStateChangedMessage : NetworkEventMessage
{
    /// <summary>Presence value (>0 means motion detected); null if the changed property was not Presence.</summary>
    public float? Presence { get; set; }

    /// <summary>Luminance value; null if the changed property was not Luminance.</summary>
    public float? Luminance { get; set; }

    public MultiSensorStateChangedMessage()
    {
        Topic = MessageTopics.NetworkEventsMultiSensorState;
        Property = "MultiSensorState";
    }

    public MultiSensorStateChangedMessage(byte sourceNodeId, float? presence, float? luminance) : this()
    {
        SourceNodeId = sourceNodeId;
        Presence = presence;
        Luminance = luminance;
    }
}
