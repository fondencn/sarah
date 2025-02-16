using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;

namespace Sarah.Data.Models
{
    [Table("Persons")]
    public class PersonInfo : IPerson
    {
        [Column]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        [Display(Name = "Name")]
        [Column]
        public string? Name { get; set; }

        [Display(Name = "Bezeichnung des Mobiltelefons")]
        [Column]
        public string? MobilePhoneHostname { get; set; }

        [Display(Name = "ID des zugeordneten GPS Tracker")]
        [Column]
        public byte GPSTrackerID { get; set; }


        [NotMapped]
        public bool IsAtHome {get; set; }

        [NotMapped]
        public string? TrackerDeviceName {get;set;}

        [NotMapped]
        public IGPSTracker? GPSTracker { get; set; }

        [NotMapped]
        public IGeoFence? CurrentGeoFence { get; set; }

        // [Display(Name = "IP Mobilgerät")]
        // [NotMapped]
        // public string MobilePhoneIP => InteLuk.HomeNet.HomeNetwork.Instance.KnownHosts?
        //     .FirstOrDefault(item => String.Equals(item.Hostname, this.MobilePhoneHostname, StringComparison.OrdinalIgnoreCase))
        //     ?.IP;

        // [Display(Name = "MAC Mobilgerät")]
        // [NotMapped]
        // public string MobilePhoneMAC => InteLuk.HomeNet.HomeNetwork.Instance.KnownHosts?
        //     .FirstOrDefault(item => String.Equals(item.Hostname, this.MobilePhoneHostname, StringComparison.OrdinalIgnoreCase))
        //     ?.MAC;

        // [Display(Name = "Anwesend")]
        // [NotMapped]
        // public bool IsActive => PersonMonitor.Instance.IsPresent(this.Name);

        // [Display(Name = "Letzte Änderung")]
        // [NotMapped]
        // public DateTime? LastActiveChanged => InteLuk.HomeNet.HomeNetwork.Instance.KnownHosts?
        //     .FirstOrDefault(item => String.Equals(item.Hostname, this.MobilePhoneHostname, StringComparison.OrdinalIgnoreCase))
        //     ?.LastConnectedStateChanged;

        // [Display(Name = "Positionsverlauf")]
        // [NotMapped]
        // public LocationServiceEntry[] Trace { get; internal set; }



        // [Display(Name = "LocationServiceLastUpdate")]
        // [NotMapped]
        // public DateTime LocationServiceLastUpdate { get; internal set; }


        // [Display(Name = "GPSTrackerName")]
        // [NotMapped]
        // public string GPSTrackerName { get; internal set; }

        // [Display(Name = "GPSTrackerLastUpdate")]
        // [NotMapped]
        // public DateTime GPSTrackerLastUpdate { get; internal set; }


        // public ILocationClientSettings LocationSettings { get; } = new LocationServiceSettings();

        // private class LocationServiceSettings : ILocationClientSettings
        // {
        //     public string Url { get; } = "https://pi:5002";
        // }


        // internal async Task LoadLocationTrace(Data.ApplicationDbContext db)
        // {
        //     try
        //     {
        //         InteLuk.LocationServiceClient.LocationServiceClient client = new LocationServiceClient.LocationServiceClient(this.LocationSettings);
        //         IEnumerable<LocationServiceEntry> personTrace = await client.GetLocationTrace(this.Name, LocationServiceApiKey.ApiKey);
        //         this.LocationServiceLastUpdate = personTrace?.Max(item => item.DateTime) ?? DateTime.MinValue;


        //         /* Dann via GPS Tracker probieren */
        //         if (this.GPSTrackerID != 0)
        //         {
        //             var tracker = db.Devices.FirstOrDefault(item => item.Id == this.GPSTrackerID);
        //             if (tracker != null)
        //             {
        //                 this.GPSTrackerName = tracker.Name;
        //                 IGPSTracker trackerElement = tracker.NetworkElement as IGPSTracker;
        //                 var trackerTrace = trackerElement.PositionTrace?
        //                     .Where (item => item.IsValid)
        //                     .Select(item => new LocationServiceEntry()
        //                 {
        //                     Battery = (trackerElement.Battery?.Value).GetValueOrDefault(0f),
        //                     Longtitude = (trackerElement.Position?.Longtitude?.Value).GetValueOrDefault(0f),
        //                     Latitude = (trackerElement.Position?.Latitude?.Value).GetValueOrDefault(0f),
        //                     DateTime = trackerElement.LastMessageReceived,
        //                 });
        //                 this.GPSTrackerLastUpdate = trackerTrace?.Max(item => item.DateTime) ?? DateTime.MinValue;
        //                 if (personTrace?.Any() == true)
        //                 {
        //                     personTrace = personTrace.Concat(trackerTrace ?? new LocationServiceEntry[0]);
        //                 } 
        //                 else
        //                 {
        //                     personTrace = trackerTrace;
        //                 }
        //             }
        //         }


        //         this.Trace = personTrace?.OrderByDescending(item => item.DateTime).ToArray();

        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Instance.LogDebug("Fehler beim laden der Geolokationsdaten von " + this.Name + " : " + ex.Message);
        //     }
        // }
    }
}
