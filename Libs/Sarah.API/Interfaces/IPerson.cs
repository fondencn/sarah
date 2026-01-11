using Sarah.API.BusinessObjects;

namespace Sarah.API.Interfaces
{
    public interface IPerson
    {
        long Id { get; set; }
        string Name { get; set; }
        byte GPSTrackerID { get; set; }
        string MobilePhoneHostname { get; set; }
        bool IsAtHome { get; set; } 
        string TrackerDeviceName {get;set;}
        IGPSTracker? GPSTracker { get; set; }
        IGeoFence? CurrentGeoFence {get;set;}
    }

}