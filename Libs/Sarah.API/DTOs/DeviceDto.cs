using System;
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
    }

    public class PositionDto
    {
        public float Longitude { get; set; }
        public float Latitude { get; set; }
        public DateTime MeasureTime { get; set; }
        public bool IsValid { get; set; }
    }
}
