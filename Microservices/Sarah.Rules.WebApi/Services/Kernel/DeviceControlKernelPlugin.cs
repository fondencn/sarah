using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

public sealed class DeviceControlKernelPlugin(DeviceServiceClient _deviceService)
{
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


    [KernelFunction, Description("Schaltet eine Lampe per Node-ID aus.")]
    public async Task<string> TurnLampOffByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampBrightness((byte)nodeId, 0);
        return $"Lampe Node {nodeId} ausgeschaltet.";
    }


    [KernelFunction, Description("Schaltet eine Lampe per Node-ID ein.")]
    public async Task<string> TurnLampOnByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampBrightness((byte)nodeId, 255);
        return $"Lampe Node {nodeId} eingeschaltet.";
    }

    
    [KernelFunction, Description("Schaltet ein Gerät (Wallplug, Steckdose) per Node-ID ein.")]
    public async Task<string> TurnDeviceOnByNodeAsync(int nodeId)
    {
        await _deviceService.SetWallPlugStateByNodeAsync((byte)nodeId, true);
        return $"Gerät Node {nodeId} eingeschaltet.";
    }


    [KernelFunction, Description("Schaltet ein Gerät (Wallplug, Steckdose) per Node-ID aus.")]
    public async Task<string> TurnDeviceOffByNodeAsync(int nodeId)
    {
        await _deviceService.SetWallPlugStateByNodeAsync((byte)nodeId, false);
        return $"Gerät Node {nodeId} ausgeschaltet.";
    }


    [KernelFunction, Description("Setzt die Ziel-Temperatur für eine Heizung anhand der Device-ID.")]
    public async Task<string> SetThermostatTemperatureAsync(long deviceId, double temperature)
    {
        await _deviceService.SetThermostatTemperatureAsync(deviceId, (float)temperature);
        return $"Thermostat {deviceId} auf {temperature:F1} Grad gesetzt.";
    }


    [KernelFunction, Description("Schaltet eine Heizung anhand der Device-ID aus.")]
    public async Task<string> TurnOffThermostatAsync(long deviceId)
    {
        double temperature = 12.0;
        await _deviceService.SetThermostatTemperatureAsync(deviceId, (float)temperature);
        return $"Thermostat {deviceId} auf {temperature:F1} Grad gesetzt.";
    }
}
