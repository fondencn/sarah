using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions ContentJsonOptions = CreateContentJsonOptions();

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

    [Column(TypeName = "int")]
    [Display(Name = "Alarmtyp")]
    [Required]
    public AlarmContentType ContentType { get; set; } = AlarmContentType.Text;

    [Column(TypeName = "jsonb")]
    [Display(Name = "Content")]
    public string? ContentJson { get; set; }

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

    [NotMapped]
    public AlarmContentDto? Content
    {
        get => DeserializeContent();
        set => SetContent(value);
    }

    [NotMapped]
    public bool IsTemperatureSchedule => ContentType == AlarmContentType.TemperatureSchedule;

    [NotMapped]
    public bool IsTextSchedule => ContentType == AlarmContentType.Text;

    public void SetTextContent(string text, string? targetSpeaker, SpeechVolume volume)
    {
        SetContent(new AlarmContentDto
        {
            Type = AlarmContentType.Text,
            Text = text,
            TargetSpeaker = targetSpeaker,
            Volume = (int)volume
        });
    }

    public void SetTemperatureContent(long? roomId, double targetTemperature, int dayOfWeek, bool suppressDuringSummer = true)
    {
        SetContent(new AlarmContentDto
        {
            Type = AlarmContentType.TemperatureSchedule,
            RoomId = roomId,
            TargetTemperature = targetTemperature,
            DayOfWeek = dayOfWeek,
            SuppressDuringSummer = suppressDuringSummer,
            Text = $"Temperatur {targetTemperature:0.#}°C"
        });
    }

    public string GetDisplayText()
    {
        return Content?.DisplayText ?? Text ?? string.Empty;
    }

    private AlarmContentDto? DeserializeContent()
    {
        if (string.IsNullOrWhiteSpace(ContentJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AlarmContentDto>(ContentJson, ContentJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void SetContent(AlarmContentDto? value)
    {
        if (value == null)
        {
            ContentJson = null;
            ContentType = AlarmContentType.Text;
            return;
        }

        ContentType = value.Type;
        ContentJson = JsonSerializer.Serialize(value, ContentJsonOptions);
        if (!string.IsNullOrWhiteSpace(value.Text))
        {
            Text = value.Text;
        }
        else if (value.Type == AlarmContentType.TemperatureSchedule && value.TargetTemperature.HasValue)
        {
            Text = $"Temperatur {value.TargetTemperature:0.#}°C";
        }
    }

    private static JsonSerializerOptions CreateContentJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

public enum AlarmScheduleMode
{
    Erinnerung,
    Geburtstag
}
