using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services.Kernel;

public sealed class SpeechKernelPlugin
{
    private readonly RabbitMQClient _rabbitMq;
    private readonly SmartHomeKernelService.ConversationSpeechContext? _speechContext;
    private readonly ILogger<SpeechKernelPlugin> _logger;

    internal SpeechKernelPlugin(
        RabbitMQClient rabbitMq,
        SmartHomeKernelService.ConversationSpeechContext? speechContext,
        ILogger<SpeechKernelPlugin> logger)
    {
        _rabbitMq = rabbitMq;
        _speechContext = speechContext;
        _logger = logger;
    }

    [KernelFunction, Description("Gibt einen Sprachtext auf einem Speaker oder auf allen Speakern aus.")]
    public async Task<string> SayAsync(
        [Description("Der zu sprechende deutsche Text.")] string text,
        [Description("Optionaler Ziel-Speaker, leer fuer Broadcast.")] string targetSpeaker = "",
        [Description("Lautstaerke: Silent, Quiet, Normal, Loud oder VeryLoud.")] string volume = "Normal")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Keine Sprachausgabe ausgefuehrt.";
        }

        if (!Enum.TryParse<SpeechVolume>(volume, ignoreCase: true, out var parsedVolume))
        {
            parsedVolume = SpeechVolume.Normal;
        }

        string resolvedTargetSpeaker = ResolveTargetSpeaker(targetSpeaker, _speechContext?.TargetSpeaker);
        await _rabbitMq.PublishAsync(new SayMessage(text, resolvedTargetSpeaker, parsedVolume));
        _speechContext?.MarkSpeechOutput();

        _logger.LogInformation("Kernel speech output published for speaker '{Speaker}'", resolvedTargetSpeaker);
        return $"Sprachausgabe gesendet: {text}";
    }

    internal static string ResolveTargetSpeaker(string requestedTargetSpeaker, string? fallbackTargetSpeaker)
    {
        if (!string.IsNullOrWhiteSpace(requestedTargetSpeaker))
        {
            return requestedTargetSpeaker.Trim();
        }

        return string.IsNullOrWhiteSpace(fallbackTargetSpeaker)
            ? string.Empty
            : fallbackTargetSpeaker.Trim();
    }
}
