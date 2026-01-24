namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message to start audio playback on a speaker
/// </summary>
public class StartAudioMessage : AbstractMessage
{
    /// <summary>
    /// Path or name of the audio file to play
    /// </summary>
    public string AudioFileName { get; set; } = string.Empty;

    /// <summary>
    /// Target speaker name (empty = all speakers)
    /// </summary>
    public string TargetSpeaker { get; set; } = string.Empty;

    public StartAudioMessage()
    {
        Topic = "speech.audio.start";
    }

    public StartAudioMessage(string audioFileName, string targetSpeaker = "") : this()
    {
        AudioFileName = audioFileName;
        TargetSpeaker = targetSpeaker;
    }
}
