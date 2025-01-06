using Sarah.API.BusinessObjects;
using Sarah.API.Extensions;
using Newtonsoft.Json;
using Sarah.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Data.Models
{
    /// <summary>
    /// Eine geplante Erinnerung
    /// </summary>
    [Table("AlarmSchedules")]
    [DateInFutureOrRecurrence]
    public class AlarmSchedule
    {

        /// <summary>
        /// ID (Autowert)
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
        public SpeechVolume Volume { get; set; } = SpeechVolume.Normal;


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
                    this.Recurrence = null;
                }
                else
                {
                    this.Recurrence = new AlarmScheduleRecurrence();
                }
            }
        }


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


        [NotMapped]
        public AlarmScheduleRecurrence? Recurrence
        {
            get => this.SerializedRecurrence != null ? JsonConvert.DeserializeObject<AlarmScheduleRecurrence>(this.SerializedRecurrence) : null;
            set { this.SerializedRecurrence = value != null ? JsonConvert.SerializeObject(value) : null; }
        }

        [Column]
        public string? SerializedRecurrence { get; set; }

        [Column(TypeName = "int")]
        [EnumDataType(typeof(AlarmScheduleMode))]
        [Display(Name = "Terminart")]
        public AlarmScheduleMode Mode { get; set; }

        /// <summary>
        /// Ermittelt das nächste Auftreten für Recurrence bzw. den Termin selbst bei Einzelterminen
        /// oder NULL falls das nächste Auftreten nicht im Gültigkeitsbereich liegt
        /// oder NULL falls das einzelne auftreten in der Vergangenheit liegt.
        /// </summary>
        /// <returns>Nächstes Auftreten des Termins sofern ermittelbar</returns>
        public DateTime? GetNextOccurence()
        {
            DateTime? next = this.AlarmTime;

            if (this.Recurrence != null && this.HasRecurrence && next.HasValue)
            {

                /* Nächsten passenden Wochentag finden, damit wir einen Aufsetzpunkt haben */
                while (!next.Value.IsWeekdayMatching(this.Recurrence.Weekdays))
                {
                    /* Rückwärts gehen bis zum 1. passenden Wochentag */
                    next = next.Value.AddDays(-1);
                }


                while (!next.Value.IsInFuture() || !next.Value.IsWeekdayMatching(this.Recurrence.Weekdays))
                {
                    next = next.Value.GetNextOccurrence(this.Recurrence.Interval);
                }
                return next;
            }
            else if (!next.Value.IsInFuture())
            {
                return null;
            }
            else
            {
                return next;
            }
        }
    }

    public enum AlarmScheduleMode
    {
        Erinnerung = 0,
        Systemaufgabe = 1
    }

    [NotMapped]
    //[DateRangeValid(nameof(From), nameof(Until))] // passiert bereits in IValidatableObject
    public class AlarmScheduleRecurrence : IValidatableObject
    {
        [Display(Name = "Start der Serie")]
        public DateTime? From { get; set; }

        [Display(Name = "Ende der Serie")]
        public DateTime? Until { get; set; }

        [EnumDataType(typeof(RecurrenceInterval))]
        public RecurrenceInterval Interval { get; set; }

        [NotMapped]
        [Display(Name = "Wiederholung")]
        [EnumDataType(typeof(RecurrenceInterval))]
        public RecurrenceInterval? IntervalViewModel
        {
            get => Interval;
            set { Interval = value.HasValue ? value.Value : RecurrenceInterval.Jährlich; }
        }

        [Display(Name = "Wochentage")]
        [EnumDataType(typeof(Weekdays))]
        public Weekdays Weekdays { get; set; }

        [NotMapped]
        [Display(Name = "Wochentage")]
        public Weekdays[] WeekdayList
        {
            get
            {
                List<Weekdays> lst = new List<Weekdays>();
                if(this.Weekdays.HasFlag(Weekdays.Montag))
                {
                    lst.Add(Weekdays.Montag);
                }
                if (this.Weekdays.HasFlag(Weekdays.Dienstag))
                {
                    lst.Add(Weekdays.Dienstag);
                }
                if (this.Weekdays.HasFlag(Weekdays.Mittwoch))
                {
                    lst.Add(Weekdays.Mittwoch);
                }
                if (this.Weekdays.HasFlag(Weekdays.Donnerstag))
                {
                    lst.Add(Weekdays.Donnerstag);
                }
                if (this.Weekdays.HasFlag(Weekdays.Freitag))
                {
                    lst.Add(Weekdays.Freitag);
                }
                if (this.Weekdays.HasFlag(Weekdays.Samstag))
                {
                    lst.Add(Weekdays.Samstag);
                }
                if (this.Weekdays.HasFlag(Weekdays.Sonntag))
                {
                    lst.Add(Weekdays.Sonntag);
                }
                return lst.ToArray();
            }
            set
            {
                if(value.Contains(Weekdays.Montag))
                {
                    this.Weekdays |= Weekdays.Montag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Montag;
                }
                if (value.Contains(Weekdays.Dienstag))
                {
                    this.Weekdays |= Weekdays.Dienstag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Dienstag;
                }
                if (value.Contains(Weekdays.Mittwoch))
                {
                    this.Weekdays |= Weekdays.Mittwoch;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Mittwoch;
                }
                if (value.Contains(Weekdays.Donnerstag))
                {
                    this.Weekdays |= Weekdays.Donnerstag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Donnerstag;
                }
                if (value.Contains(Weekdays.Freitag))
                {
                    this.Weekdays |= Weekdays.Freitag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Freitag;
                }
                if (value.Contains(Weekdays.Samstag))
                {
                    this.Weekdays |= Weekdays.Samstag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Samstag;
                }
                if (value.Contains(Weekdays.Sonntag))
                {
                    this.Weekdays |= Weekdays.Sonntag;
                }
                else
                {
                    this.Weekdays &= ~Weekdays.Sonntag;
                }
            }
        }

        [NotMapped]
        [Display(Name = "Montags")]
        [Required]
        public bool IsMonday
        {
            get => Weekdays.HasFlag(Weekdays.Montag);
            set { if (value) Weekdays |= Weekdays.Montag; else Weekdays &= ~Weekdays.Montag; }
        }

        [NotMapped]
        [Required]
        [Display(Name = "Dienstags")]
        public bool IsTuesday
        {
            get => Weekdays.HasFlag(Weekdays.Dienstag);
            set { if (value) Weekdays |= Weekdays.Dienstag; else Weekdays &= ~Weekdays.Dienstag; }
        }

        [NotMapped]
        [Display(Name = "Mittwochs")]
        [Required]
        public bool IsWednesday
        {
            get => Weekdays.HasFlag(Weekdays.Mittwoch);
            set { if (value) Weekdays |= Weekdays.Mittwoch; else Weekdays &= ~Weekdays.Mittwoch; }
        }

        [NotMapped]
        [Display(Name = "Donnerstags")]
        [Required]
        public bool IsThursday
        {
            get => Weekdays.HasFlag(Weekdays.Donnerstag);
            set { if (value) Weekdays |= Weekdays.Donnerstag; else Weekdays &= ~Weekdays.Donnerstag; }
        }

        [NotMapped]
        [Display(Name = "Freitags")]
        [Required]
        public bool IsFriday
        {
            get => Weekdays.HasFlag(Weekdays.Freitag);
            set { if (value) Weekdays |= Weekdays.Freitag; else Weekdays &= ~Weekdays.Freitag; }
        }

        [NotMapped]
        [Display(Name = "Samstags")]
        [Required]
        public bool IsSaturday
        {
            get => Weekdays.HasFlag(Weekdays.Samstag);
            set { if (value) Weekdays |= Weekdays.Samstag; else Weekdays &= ~Weekdays.Samstag; }
        }

        [NotMapped]
        [Display(Name = "Sonntags")]
        [Required]
        public bool IsSunday
        {
            get => Weekdays.HasFlag(Weekdays.Sonntag);
            set { if (value) Weekdays |= Weekdays.Sonntag; else Weekdays &= ~Weekdays.Sonntag; }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            if (From.HasValue && Until.HasValue)
            {
                if(Until <= From)
                {
                    results.Add(new ValidationResult("Das Enddatum darf nicht vor dem Startdatum liegen"));
                }
            }
            if(!IntervalViewModel.HasValue)
            {
                results.Add(new ValidationResult("Es muss ein Intervall angegeben werden"));
            }
            return results;
        }
    }

}
