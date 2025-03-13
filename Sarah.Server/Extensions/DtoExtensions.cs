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
    public static DeviceDto? ToDto(this DeviceInfo device, IDeviceService deviceService) 
    {
        var dto =  device == null ? null : new DeviceDto {
            Id = device.Id,
            NodeId = device.NodeID,
            TypeName = device.SpecificType.ToString() + "|" + (device.GetNetworkItem(deviceService)?.GetType().Name ?? "Unknown type"),
            Name = device.Name,
            Info = (device.GetNetworkItem(deviceService)?.StateInfo) ?? "Unknown state",
            DeviceType = device.SpecificType,
            RoomId = device.Id_Room, 
            IsReadonly = device.IsReadonly
        };

        if(dto != null) 
        {
            dto.ExtendedProperties = device?.GetNetworkItem(deviceService)?.ReadObjPropertiesAsJson();
        }
        return dto;
    }

    /// <summary>
    /// Converts a <see cref="Room"/> object to a <see cref="RoomDto"/> object.
    /// </summary>
    public static DeviceInfo ToEntity(this DeviceDto dto) 
    {
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

    public static IPerson ToEntity(this PersonDto dto) {
        return new PersonInfo { Id = dto.Id, Name = dto.Name ?? "", GPSTrackerID = dto.GPSTrackerID, MobilePhoneHostname = dto.MobilePhoneHostname }; 
    }

    /// <summary>
    /// Converts a <see cref="Room"/> object to a <see cref="RoomDto"/> object.
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public static RoomDto ToDto(this Room r, bool isFavourite) {
        return new RoomDto { Id = r.Id, Name = (r.Name ?? String.Empty), IsFavourite = isFavourite };
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

    public static PersonDto ToDto(this IPerson e, bool isFavourite) {
        return new PersonDto 
        { 
            Id = e.Id, 
            Name = e.Name, 
            GPSTrackerID = e.GPSTrackerID, 
            MobilePhoneHostname = e.MobilePhoneHostname ,
            IsAtHome = e.IsAtHome,
            GPSTrackerName = e.TrackerDeviceName,
            CurrentGeoFence = e.CurrentGeoFence?.Name,
            CurrentPosition = e.GPSTracker?.Position?.ToString() ?? "Position unknown",
            IsFavourite = isFavourite
        };
    }


        /// <summary>
        ///  Read the public, non-static properties of an object and return them as a ExtendedPropertyDto[]
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        internal static ExtendedPropertyDto[] ReadObjPropertiesAsJson(this object? item)
        {
            List<ExtendedPropertyDto> result = new List<ExtendedPropertyDto>();

            if (item != null) 
            {
                foreach(var property in item.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                   result.Add(new ExtendedPropertyDto() {Key = property.Name, Value = property.GetValue(item)?.ToString()  ?? ""});
                }
            }

            return result.ToArray();
        }
}