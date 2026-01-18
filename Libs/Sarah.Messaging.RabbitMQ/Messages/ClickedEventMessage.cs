namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a hardware button is pressed
/// </summary>
public class ClickedEventMessage : NetworkEventMessage
{
    /// <summary>
    /// Scene ID of the button press
    /// </summary>
    public byte SceneId { get; set; }

    public ClickedEventMessage()
    {
        Topic = "network.events.clicked";
        Property = "ButtonClicked";
    }

    public ClickedEventMessage(byte sourceNodeId, byte sceneId) : this()
    {
        SourceNodeId = sourceNodeId;
        SceneId = sceneId;
    }
}
