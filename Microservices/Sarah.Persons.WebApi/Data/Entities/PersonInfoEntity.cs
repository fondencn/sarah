using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sarah.API.Interfaces;

namespace Sarah.Persons.WebApi.Data.Entities;

[Table("Persons")]
public class PersonInfoEntity : IPerson
{
    [Column]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long Id { get; set; }

    [Display(Name = "Name")]
    [Column]
    public string Name { get; set; } = "";

    [Display(Name = "Bezeichnung des Mobiltelefons")]
    [Column]
    public string MobilePhoneHostname { get; set; } = "";

    [Display(Name = "ID des zugeordneten GPS Tracker")]
    [Column]
    public byte GPSTrackerID { get; set; }

    [NotMapped]
    public bool IsAtHome { get; set; }

    [NotMapped]
    public string TrackerDeviceName { get; set; } = "";

    [NotMapped]
    public IGPSTracker? GPSTracker { get; set; }

    [NotMapped]
    public IGeoFence? CurrentGeoFence { get; set; }
}
