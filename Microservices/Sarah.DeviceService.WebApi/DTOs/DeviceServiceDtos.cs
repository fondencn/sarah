using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System;
using System.Collections.Generic;

namespace Sarah.DeviceService.WebApi.DTOs
{
    public class NodeDto
    {
        public string? DeviceTypeName { get; set; }
    }

    public class NetworkElementDto
    {
        public byte NodeID { get; set; }
        public string? StateInfo { get; set; }
        public string? ClassDescription { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SensorDataDto
    {
        public float Value { get; set; }
        public string? Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LampDto : NetworkElementDto
    {
        public DateTime? LastChange { get; set; }
        public byte Brightness { get; set; }
        public string? Color { get; set; }
        public string? CurrentAnimation { get; set; }
    }

    public class WallPlugDto : NetworkElementDto
    {
        public bool IsOn { get; set; }
        public DateTime LastChangeToPowerLow { get; set; }
        public DateTime LastIncreasePower { get; set; }
        public DateTime LastDecreasePower { get; set; }
    }

    public class MultiSensorDto : NetworkElementDto
    {
        public SensorDataDto? Luminance { get; set; }
        public SensorDataDto? Presence { get; set; }
        public SensorDataDto? RelativeHumidity { get; set; }
        public SensorDataDto? VolatileOrganicCompounds { get; set; }
        public SensorDataDto? CO2 { get; set; }
    }

    public class ThermoElementDto : NetworkElementDto
    {
        public SensorDataDto? TemperatureSetpoint { get; set; }
    }

    public class DoorSensorDto : NetworkElementDto
    {
        public DoorSensorState State { get; set; }
        public DateTime? LastStateChanged { get; set; }
        public TimeSpan? LastOpenDuration { get; set; }
    }

    public class SmokeSensorDto : NetworkElementDto
    {
        public SensorDataDto? IsSmokeDetected { get; set; }
        public SensorDataDto? IsOverheatingDetected { get; set; }
        public SensorDataDto? Alarm { get; set; }
    }

    public class BatterySensorDto
    {
        public byte NodeID { get; set; }
        public SensorDataDto? Battery { get; set; }
    }

    public class ControllerElementDto : NetworkElementDto
    {
    }

    public class WallControllerDto : NetworkElementDto
    {
        public byte LastSceneId { get; set; }
    }

    public class UnknownElementDto : NetworkElementDto
    {
    }

    public class GpsTrackerDto : NetworkElementDto
    {
        public SensorDataDto? Battery { get; set; }
        public LocatorPositionDto? Position { get; set; }
        public IEnumerable<LocatorPositionDto>? PositionTrace { get; set; }
        public DateTime LastMessageReceived { get; set; }
        public SensorDataDto? IsButtonPressed { get; set; }
    }

    public class LocatorPositionDto
    {
        public SensorDataDto? Longtitude { get; set; }
        public SensorDataDto? Latitude { get; set; }
        public DateTime MeasureTime { get; set; }
        public bool IsValid { get; set; }
    }

    public class AssociationGroupDto
    {
        public byte GroupID { get; set; }
        public byte[]? Nodes { get; set; }
        public byte MaxNodesSupported { get; set; }
    }

    public class ParameterProviderDto
    {
        public string? Name { get; set; }
    }
}
