using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.DeviceService.WebApi.Data.Entities;

[Table("Rooms")]
public class RoomEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column]
    public long Id { get; set; }

    [Display(Name = "Raumbezeichnung")]
    [Column]
    public string? Name { get; set; }
}
