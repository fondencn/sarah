namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when the SOS/button state changes on a GPS tracker.
/// </summary>
public class TrackerButtonPressedMessage : NetworkEventMessage
{
    /// <summary>
    /// True when the button is pressed; false when released.
    /// </summary>
    public bool IsPressed { get; set; }

    public TrackerButtonPressedMessage()
    {
        Topic = MessageTopics.NetworkEventsTrackerButton;
        Property = "TrackerButton";
    }

    public TrackerButtonPressedMessage(byte sourceNodeId, bool isPressed) : this()
    {
        this.SourceNodeId = sourceNodeId;
        this.IsPressed = isPressed;
    }
}
