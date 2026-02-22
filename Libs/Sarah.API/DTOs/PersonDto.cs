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
    }
}
