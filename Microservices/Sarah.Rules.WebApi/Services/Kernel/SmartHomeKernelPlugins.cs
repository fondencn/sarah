using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

public sealed class SpeechKernelPlugin
{
    private readonly RabbitMQClient _rabbitMq;
    private readonly ILogger<SpeechKernelPlugin> _logger;

    public SpeechKernelPlugin(RabbitMQClient rabbitMq, ILogger<SpeechKernelPlugin> logger)
    {
        _rabbitMq = rabbitMq;
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

        await _rabbitMq.PublishAsync(new SayMessage(text, targetSpeaker, parsedVolume));
        _logger.LogInformation("Kernel speech output published for speaker '{Speaker}'", targetSpeaker);
        return $"Sprachausgabe gesendet: {text}";
    }
}

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

public sealed class DeviceControlKernelPlugin
{
    private readonly DeviceServiceClient _deviceService;

    public DeviceControlKernelPlugin(DeviceServiceClient deviceService)
    {
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Aktiviert eine Szene anhand ihres Namens.")]
    public async Task<string> ActivateSceneAsync(string sceneName)
    {
        await _deviceService.ActivateScene(sceneName);
        return $"Szene aktiviert: {sceneName}";
    }

    [KernelFunction, Description("Deaktiviert eine Szene anhand ihres Namens.")]
    public async Task<string> DeactivateSceneAsync(string sceneName)
    {
        await _deviceService.DeactivateScene(sceneName);
        return $"Szene deaktiviert: {sceneName}";
    }

    [KernelFunction, Description("Schaltet eine Lampe per Node-ID um.")]
    public async Task<string> ToggleLampByNodeAsync(int nodeId)
    {
        await _deviceService.ToggleLampByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} umgeschaltet.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf warmweiss.")]
    public async Task<string> SetLampWarmWhiteByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampWarmWhiteByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} auf warmweiss gesetzt.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf kaltweiss.")]
    public async Task<string> SetLampColdWhiteByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampColdWhiteByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} auf kaltweiss gesetzt.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf Farbe und Helligkeit.")]
    public async Task<string> SetLampColorAndBrightnessByNodeAsync(int nodeId, string color, int brightness)
    {
        await _deviceService.SetLampColorAndBrightnessByNodeAsync((byte)nodeId, color, (byte)brightness);
        return $"Lampe Node {nodeId} auf Farbe {color} und Helligkeit {brightness} gesetzt.";
    }

    [KernelFunction, Description("Schaltet einen WallPlug per Node-ID ein oder aus.")]
    public async Task<string> SetWallPlugStateByNodeAsync(int nodeId, bool isOn)
    {
        await _deviceService.SetWallPlugStateByNodeAsync((byte)nodeId, isOn);
        return $"WallPlug Node {nodeId} auf {(isOn ? "an" : "aus")} gesetzt.";
    }

    [KernelFunction, Description("Schaltet einen WallPlug per Node-ID um.")]
    public async Task<string> ToggleWallPlugByNodeAsync(int nodeId)
    {
        await _deviceService.ToggleWallPlugByNodeAsync((byte)nodeId);
        return $"WallPlug Node {nodeId} umgeschaltet.";
    }

    [KernelFunction, Description("Setzt die Thermostat-Temperatur fuer ein Device anhand der Device-ID.")]
    public async Task<string> SetThermostatTemperatureAsync(long deviceId, double temperature)
    {
        await _deviceService.SetThermostatTemperatureAsync(deviceId, (float)temperature);
        return $"Thermostat {deviceId} auf {temperature:F1} Grad gesetzt.";
    }
}