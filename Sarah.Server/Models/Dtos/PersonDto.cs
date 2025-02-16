namespace Sarah.Server.Models.Dtos
{
    public class PersonDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public byte GPSTrackerID { get; set; }
        public string? GPSTrackerName { get; set; }
        public string? CurrentGeoFence { get; set; }
        public string? CurrentPosition { get; set; }
        public bool IsAtHome  { get; set; }
        public string MobilePhoneHostname { get; set; } = "";
        public bool IsFavourite { get; set; }
    }
}