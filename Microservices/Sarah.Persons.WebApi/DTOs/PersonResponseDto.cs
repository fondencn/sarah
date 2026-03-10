namespace Sarah.Persons.WebApi.DTOs;

public class PersonResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public byte GpsTrackerID { get; set; }
    public string? GpsTrackerName { get; set; }
    public string? CurrentGeoFence { get; set; }
    public string? CurrentPosition { get; set; }
    public bool IsAtHome { get; set; }
    public string? MobilePhoneHostname { get; set; }
    public bool IsFavourite { get; set; }
}
