namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message to trigger text-to-speech on a speaker
/// </summary>
public class SayMessage : AbstractMessage
{
    /// <summary>
    /// Text to speak
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Target speaker name (empty = all speakers)
    /// </summary>
    public string TargetSpeaker { get; set; } = string.Empty;

    /// <summary>
    /// Speech volume level
    /// </summary>
    public SpeechVolume Volume { get; set; } = SpeechVolume.Normal;

    public SayMessage()
    {
        Topic = "speech.say";
    }

    public SayMessage(string message, string targetSpeaker = "", SpeechVolume volume = SpeechVolume.Normal) : this()
    {
        Message = message;
        TargetSpeaker = targetSpeaker;
        Volume = volume;
    }
}

/// <summary>
/// Speech volume levels
/// </summary>
public enum SpeechVolume
{
    Silent = 0,
    Quiet = 1,
    Normal = 2,
    Loud = 3,
    VeryLoud = 4
}
