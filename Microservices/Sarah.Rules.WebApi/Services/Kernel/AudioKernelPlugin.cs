using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services.Kernel;

public sealed class AudioKernelPlugin
{
    private readonly RabbitMQClient _rabbitMq;

    public AudioKernelPlugin(RabbitMQClient rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    [KernelFunction, Description("Startet die Wiedergabe einer Audiodatei auf einem Speaker oder als Broadcast.")]
    public async Task<string> StartAudioAsync(string audioFileName, string targetSpeaker = "")
    {
        await _rabbitMq.PublishAsync(new StartAudioMessage(audioFileName, targetSpeaker));
        return $"Audio gestartet: {audioFileName}";
    }

    [KernelFunction, Description("Stoppt die Audio-Wiedergabe auf einem Speaker oder auf allen Speakern.")]
    public async Task<string> StopAudioAsync(string targetSpeaker = "")
    {
        await _rabbitMq.PublishAsync(new StopAudioMessage(targetSpeaker));
        return "Audio gestoppt.";
    }
}
