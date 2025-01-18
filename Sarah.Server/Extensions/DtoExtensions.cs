using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Sarah.Server.Models.Dtos;

/// <summary>
/// Extension methods for converting between DTOs and entities.
/// </summary>
public static class DtoExtensions {
    /// <summary>
    /// Converts a <see cref="DeviceInfo"/> object to a <see cref="DeviceDto"/> object.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="deviceService"></param>
    /// <returns></returns>
    public static DeviceDto ToDto(this DeviceInfo device, IDeviceService deviceService) {
        return new DeviceDto {
            Id = device.Id,
            NodeId = device.NodeID,
            TypeName = device.SpecificType.ToString() + "|" + (device.GetNetworkItem(deviceService)?.GetType().Name ?? "Unknown type"),
            Name = device.Name,
            Info = (device.GetNetworkItem(deviceService)?.StateInfo) ?? "Unknown state",
            DeviceType = device.SpecificType,
            RoomId = device.Id_Room, 
            IsReadonly = device.IsReadonly
        };
    }

    /// <summary>
    /// Converts a <see cref="Room"/> object to a <see cref="RoomDto"/> object.
    /// </summary>
    public static DeviceInfo ToEntity(this DeviceDto dto) {
        return new DeviceInfo {
            Id = dto.Id,
            NodeID = dto.NodeId,
            SpecificType = dto.DeviceType,
            Name = dto.Name,
            Id_Room = dto.RoomId, 
            IsReadonly = dto.IsReadonly
        };
    }


    /// <summary>
    /// Updates a <see cref="DeviceInfo"/> object from a <see cref="DeviceDto"/> object.
    /// </summary>
    /// <param name="device">target objects to overwrite the properties</param>
    /// <param name="dto">source object</param>
    public static void UpdateFromDto(this DeviceInfo device, DeviceDto dto)
    {
        if (device == null) {
            throw new ArgumentNullException(nameof(device));
        }

        if (dto == null) {
            throw new ArgumentNullException(nameof(dto));
        }

        device.Id = dto.Id;
        device.NodeID = dto.NodeId;
        device.SpecificType = dto.DeviceType;
        device.Name = dto.Name;
        device.Id_Room = dto.RoomId;
        device.IsReadonly = dto.IsReadonly;
    }

    /// <summary>
    /// Converts a <see cref="RoomDto"/> object to a <see cref="Room"/> object.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public static Room ToEntity(this RoomDto dto) {
        return new Room { Id = dto.Id, Name = dto.Name };
    }

    /// <summary>
    /// Converts a <see cref="Enum"/> object to a <see cref="EnumDto"/> object.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="deviceService"></param>
    /// <returns></returns>
    public static INetworkElement ToEntity(this NetworkElementDto dto, IDeviceService deviceService) {
        return deviceService.GetNetworkItem(dto.Id);
    }

    /// <summary>
    /// Converts a <see cref="Room"/> object to a <see cref="RoomDto"/> object.
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public static RoomDto ToDto(this Room r) {
        return new RoomDto { Id = r.Id, Name = (r.Name ?? String.Empty)  };
    }

    /// <summary>
    /// Converts a <see cref="Enum"/> object to a <see cref="EnumDto"/> object.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static EnumDto ToDto(this Enum e) {
        return new EnumDto { EnumKey = Convert.ToInt32(e), EnumValue = e.ToString() };
    }

    /// <summary>
    /// Converts a <see cref="NetworkElement"/> object to a <see cref="NetworkElementDto"/> object.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static NetworkElementDto ToDto(this NetworkElement e) {
        return new NetworkElementDto { Id = e.NodeID, Type = e.GetType().Name };
    }
}