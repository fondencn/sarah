using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

public sealed class RoomStateKernelPlugin
{
    private readonly RoomServiceClient _roomService;
    private readonly DeviceServiceClient _deviceService;

    public RoomStateKernelPlugin(RoomServiceClient roomService, DeviceServiceClient deviceService)
    {
        _roomService = roomService;
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Liefert eine Liste aller Raeume.")]
    public async Task<string> GetAllRoomsAsync()
    {
        var rooms = (await _roomService.GetAllRoomsAsync())
            .OrderBy(r => r.Name)
            .ToList();

        if (rooms.Count == 0)
        {
            return "Keine Raeume gefunden.";
        }

        string roomList = string.Join(", ", rooms.Select(r => $"{r.Id}: {r.Name}"));
        return $"Raeume ({rooms.Count}): {roomList}.";
    }

    [KernelFunction, Description("Liefert alle Geraete in einem Raum anhand der Room-ID.")]
    public async Task<string> GetDevicesInRoomAsync(long roomId)
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        var room = rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
        {
            return $"Kein Raum mit ID {roomId} gefunden.";
        }

        var devices = (await _deviceService.GetAllDevicesAsync())
            .Where(d => d.RoomId == roomId)
            .OrderBy(d => d.Name)
            .ThenBy(d => d.NodeId)
            .ToList();

        if (devices.Count == 0)
        {
            return $"Im Raum {room.Name} (ID {roomId}) sind keine Geraete hinterlegt.";
        }

        string list = string.Join("; ", devices.Select(d => FormatDeviceShort(d)));
        return $"Geraete im Raum {room.Name} (ID {roomId}), Anzahl {devices.Count}: {list}.";
    }

    [KernelFunction, Description("Liefert den aktuellen Zustand eines Raums: Durchschnittstemperatur, offene Tueren und Praesenz.")]
    public async Task<string> GetRoomStateAsync(long roomId)
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        var room = rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
        {
            return $"Kein Raum mit ID {roomId} gefunden.";
        }

        var summary = await _deviceService.GetRoomSummaryAsync(roomId);
        if (summary == null)
        {
            return $"Kein Raumzustand fuer {room.Name} (ID {roomId}) verfuegbar.";
        }

        var devices = (await _deviceService.GetAllDevicesAsync())
            .Where(d => d.RoomId == roomId)
            .ToList();

        var openDoorDevices = devices
            .Where(d => d.DoorSensor?.State == Sarah.API.BusinessObjects.DoorSensorState.Offen)
            .Select(d => string.IsNullOrWhiteSpace(d.Name) ? $"Node {d.NodeId}" : d.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        string averageTemperature = summary.AverageTemperature.HasValue
            ? $"{summary.AverageTemperature.Value:F1} Grad"
            : "nicht verfuegbar";
        string presence = summary.AnyPresence ? "ja" : "nein";
        string openDoors = openDoorDevices.Count == 0
            ? "keine"
            : string.Join(", ", openDoorDevices);

        return $"Raumzustand {room.Name} (ID {roomId}): Temperatur {averageTemperature}, Praesenz {presence}, offene Tueren/Fenster {openDoors}.";
    }

    public async Task<string> GetOpenDoorsAsync()
    {
        var openDoors = await _deviceService.GetOpenDoors();
        if (string.IsNullOrWhiteSpace(openDoors.OpenDoorInfo))
        {
            return "Aktuell sind keine Tueren offen.";  
        } else {
            return "Offene Tueren: " + openDoors.OpenDoorInfo;
        }
    }

    private static string FormatDeviceShort(Sarah.API.BusinessObjects.DTOs.DeviceDto device)
    {
        string name = string.IsNullOrWhiteSpace(device.Name) ? "(ohne Name)" : device.Name;
        string type = string.IsNullOrWhiteSpace(device.TypeName) ? device.DeviceType.ToString() : device.TypeName;
        return $"{name} [Node {device.NodeId}, Typ {type}]";
    }
}
