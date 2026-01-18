using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Monitoring.WebApi.Data.Entities;

/// <summary>
/// A scheduled temperature setting for a room
/// </summary>
[Table("TemperatureSchedules")]
public class TemperatureScheduleEntity
{
    [Column]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column]
    [Display(Name = "Raum ID")]
    public long? Id_Room { get; set; }

    [Column]
    [Display(Name = "Startzeit")]
    public DateTime StartTime { get; set; }

    [Column]
    [Display(Name = "Zieltemperatur")]
    public double TargetTemperature { get; set; }

    [Column]
    [Display(Name = "Wochentag")]
    public int DayOfWeek { get; set; }
}
