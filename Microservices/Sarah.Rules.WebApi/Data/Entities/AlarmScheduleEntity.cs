using Sarah.API.BusinessObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Rules.Data.Entities;

/// <summary>
/// A scheduled reminder/alarm that triggers at specific times
/// </summary>
[Table("AlarmSchedules")]
public class AlarmScheduleEntity
{
    /// <summary>
    /// ID (auto-generated)
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long Id { get; set; }

    [Column]
    [Display(Name = "Zeitpunkt")]
    [Required]
    public DateTime AlarmTime { get; set; }

    [Column]
    [Display(Name = "Text")]
    [Required]
    public string? Text { get; set; }

    [Column]
    [Display(Name = "Ziel-Gerät")]
    public string? TargetSpeaker { get; set; }

    [Column]
    [Required]
    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [Column(TypeName = "int")]
    [EnumDataType(typeof(SpeechVolume))]
    [Required]
    [Display(Name = "Lautstärke")]
    public SpeechVolume Volume { get; set; }

    [Column]
    [Display(Name = "Wiederholung")]
    public bool IsRecurrence { get; set; } = false;

    [Column]
    [Display(Name = "Wiederholung aktiviert")]
    public bool RecurrenceIsEnabled { get; set; } = false;

    [Column]
    [Display(Name = "Wochentag")]
    public int RecurrenceDayOfWeek { get; set; } = 0;

    [Column]
    [Display(Name = "Uhrzeit")]
    public TimeSpan RecurrenceTime { get; set; } = TimeSpan.Zero;

    [Column]
    [Display(Name = "Nur in den Ferien")]
    public bool? IsNurInFerien { get; set; }

    [Column]
    [Display(Name = "Nicht in den Ferien")]
    public bool? IsNichtInFerien { get; set; }

    [NotMapped]
    [Display(Name = "Nur in den Ferien")]
    public bool IsNurInFerienNotNull
    {
        get => IsNurInFerien.GetValueOrDefault(false);
        set => IsNurInFerien = value;
    }

    [NotMapped]
    [Display(Name = "Nicht in den Ferien")]
    public bool IsNichtInFerienNotNull
    {
        get => IsNichtInFerien.GetValueOrDefault(false);
        set => IsNichtInFerien = value;
    }

    private bool _HasRecurrence;
    [Column]
    [Required]
    [Display(Name = "Serientermin")]
    public bool HasRecurrence
    {
        get => _HasRecurrence;
        set
        {
            _HasRecurrence = value;
            if (!_HasRecurrence)
            {
                this.SerializedRecurrence = null;
            }
        }
    }

    [Column]
    public string? SerializedRecurrence { get; set; }
}

public enum AlarmScheduleMode
{
    Erinnerung,
    Geburtstag
}
