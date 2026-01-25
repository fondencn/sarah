namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message to stop audio playback on a speaker
/// </summary>
public class StopAudioMessage : AbstractMessage
{
    /// <summary>
    /// Target speaker name (empty = all speakers)
    /// </summary>
    public string TargetSpeaker { get; set; } = string.Empty;

    public StopAudioMessage()
    {
        Topic = MessageTopics.SpeechAudioStop;
    }

    public StopAudioMessage(string targetSpeaker = "") : this()
    {
        TargetSpeaker = targetSpeaker;
    }
}
