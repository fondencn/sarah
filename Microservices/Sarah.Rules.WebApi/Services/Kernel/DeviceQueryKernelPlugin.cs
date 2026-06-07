using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;


public sealed class DeviceQueryKernelPlugin
{
    private readonly DeviceServiceClient _deviceService;

    public DeviceQueryKernelPlugin(DeviceServiceClient deviceService)
    {
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Liefert eine Uebersicht der aktuellen Geraetelandschaft inkl. Anzahl nach Geraetetyp.")]
    public async Task<string> GetDevicesSummaryAsync()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        if (devices.Count == 0)
        {
            return "Keine Geraete gefunden.";
        }

        var grouped = devices
            .GroupBy(d => string.IsNullOrWhiteSpace(d.TypeName) ? d.DeviceType.ToString() : d.TypeName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        return $"Gesamtgeraete: {devices.Count}. Typen: {string.Join("; ", grouped)}.";
    }

    [KernelFunction, Description("Liefert Details zu einem Geraet anhand der Node-ID.")]
    public async Task<string> GetDeviceByNodeIdAsync(int nodeId)
    {
        if (nodeId < byte.MinValue || nodeId > byte.MaxValue)
        {
            return $"Ungueltige Node-ID: {nodeId}.";
        }

        var device = await _deviceService.GetDeviceByNodeIdAsync((byte)nodeId);
        return device == null
            ? $"Kein Geraet fuer Node {nodeId} gefunden."
            : FormatDevice(device);
    }

    [KernelFunction, Description("Liefert Details zu einem Geraet anhand der Device-ID.")]
    public async Task<string> GetDeviceByIdAsync(long deviceId)
    {
        var device = await _deviceService.GetDeviceByIdAsync(deviceId);
        return device == null
            ? $"Kein Geraet mit ID {deviceId} gefunden."
            : FormatDevice(device);
    }

    [KernelFunction, Description("Liefert eine Liste aktuell offener Tueren.")]
    public async Task<string> GetOpenDoorsAsync()
    {
        var openDoors = await _deviceService.GetOpenDoors();
        if (string.IsNullOrWhiteSpace(openDoors.OpenDoorInfo))
        {
            return "Aktuell sind keine Tueren offen.";
        }

        return "Offene Tueren: " + openDoors.OpenDoorInfo;
    }

    private static string FormatDevice(Sarah.API.BusinessObjects.DTOs.DeviceDto device)
    {
        var name = string.IsNullOrWhiteSpace(device.Name) ? "(ohne Name)" : device.Name;
        string state;

        if (device.Lamp != null)
        {
            state = $"Lampe Helligkeit {device.Lamp.Brightness}, Farbe {device.Lamp.Color ?? "unbekannt"}";
        }
        else if (device.WallPlug != null)
        {
            state = $"WallPlug {(device.WallPlug.IsOn ? "an" : "aus")}";
        }
        else if (device.Thermostat != null)
        {
            state = $"Thermostat Solltemperatur {device.Thermostat.TemperatureSetpoint?.ToString("F1") ?? "unbekannt"}";
        }
        else if (device.DoorSensor != null)
        {
            state = $"DoorSensor Zustand {device.DoorSensor.State}";
        }
        else
        {
            state = "Kein spezifischer Zustandsblock verfuegbar";
        }

        return $"Device {device.Id}, Node {device.NodeId}, Name {name}, Typ {device.DeviceType}: {state}.";
    }
}
