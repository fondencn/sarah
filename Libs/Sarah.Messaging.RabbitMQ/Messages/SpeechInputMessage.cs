namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message containing recognized user speech text and source speaker metadata.
/// </summary>
public sealed class SpeechInputMessage : AbstractMessage
{
    /// <summary>
    /// Recognized speech text.
    /// </summary>
    public string RecognizedText { get; set; } = string.Empty;

    /// <summary>
    /// Hostname of the speaker device that recognized the speech.
    /// </summary>
    public string SpeakerHostName { get; set; } = string.Empty;

    /// <summary>
    /// Location configured on the recognizing speaker host.
    /// </summary>
    public string SpeakerLocation { get; set; } = string.Empty;

    public SpeechInputMessage()
    {
        Topic = MessageTopics.SpeechRecognized;
    }

    public SpeechInputMessage(string recognizedText, string speakerHostName, string speakerLocation) : this()
    {
        RecognizedText = recognizedText;
        SpeakerHostName = speakerHostName;
        SpeakerLocation = speakerLocation;
    }
}
