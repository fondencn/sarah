using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

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
