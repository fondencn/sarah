using System;
using System.Collections.Generic;
using Sarah.API.BusinessObjects;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class DeviceDto
    {
        public long Id { get; set; }
        public long? RoomId { get; set; }
        public string? Name { get; set; }
        public int NodeId { get; set; }
        public KnownDeviceTypes DeviceType { get; set; }
        public string? TypeName { get; set; }
        public string? Info { get; set; }
        public bool IsReadonly { get; set; }
        public bool IsFavourite { get; set; }
        
        public PositionDto? Position { get; set; }
        public DoorSensorStateDto? DoorSensor { get; set; }
        public ThermoStateDto? Thermostat { get; set; }
        public AirQualityStateDto? AirQuality { get; set; }
        public List<ExtendedPropertyDto>? ExtendedProperties { get; set; }
    }

    public class ThermoStateDto
    {
        public float? TemperatureSetpoint { get; set; }
    }

    public class AirQualityStateDto
    {
        public float? CO2 { get; set; }
        public float? VolatileOrganicCompounds { get; set; }
        public float? RelativeHumidity { get; set; }
    }

    public class DoorSensorStateDto
    {
        public DoorSensorState State { get; set; }
        public DateTime? LastStateChanged { get; set; }
    }

    public class PositionDto
    {
        public float Longitude { get; set; }
        public float Latitude { get; set; }
        public DateTime MeasureTime { get; set; }
        public bool IsValid { get; set; }
    }
}
