using Sarah.API.Interfaces;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class PersonDto : IPerson
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string MobilePhoneHostname { get; set; } = "";
        public byte GPSTrackerID { get; set; }
        
        public bool IsAtHome { get; set; }
        public string TrackerDeviceName { get; set; } = "";
        public IGPSTracker? GPSTracker { get; set; }
        public IGeoFence? CurrentGeoFence { get; set; }
        public string CurrentPosition { get; set; } = "";
        public float? CurrentPositionLong { get; set; }
        public float? CurrentPositionLat { get; set; }
        public string? CurrentNamedPosition { get; set; }
    }
}
