using Sarah.API.BusinessObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Monitoring.WebApi.Data.Entities;

[Table("Devices")]
public class DeviceInfoEntity
{
    [Column]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long Id { get; set; }

    [Column]
    public long? Id_Room { get; set; }

    [Display(Name = "Gerätename")]
    [Column]
    public string? Name { get; set; }

    [Display(Name = "Z-Wave Node ID")]
    [Column]
    public byte NodeID { get; set; }

    [Display(Name = "Geräteklasse")]
    [Column(TypeName = "int")]
    [EnumDataType(typeof(KnownDeviceTypes))]
    public KnownDeviceTypes SpecificType { get; set; }

    [Display(Name = "Schreibgeschützt")]
    [Column]
    public bool IsReadonly { get; set; }
}
