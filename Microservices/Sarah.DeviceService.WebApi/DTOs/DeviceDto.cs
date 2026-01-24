namespace Sarah.DeviceService.WebApi.DTOs;

public class DeviceDto
{
    public long Id { get; set; }
    public long? RoomId { get; set; }
    public string? Name { get; set; }
    public byte NodeID { get; set; }
    public string? DeviceType { get; set; }
    public bool IsReadonly { get; set; }
    
    // GPS Tracker specific properties
    public PositionDto? Position { get; set; }
}

public class PositionDto
{
    public float Longitude { get; set; }
    public float Latitude { get; set; }
    public DateTime MeasureTime { get; set; }
    public bool IsValid { get; set; }
}
