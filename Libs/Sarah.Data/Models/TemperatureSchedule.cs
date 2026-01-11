using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;

namespace Sarah.Data.Models
{
    /// <summary>
    /// Eine geplante Temperatureinstellung für einen Raum
    /// </summary>
    [Table("TemperatureSchedules")]
    public class TemperatureSchedule
    {
        /// <summary>
        /// Start der Sommerzeit-Schaltung für die Heizung
        /// </summary>
        private static DateTime SummertimeStart => new DateTime(DateTime.Now.Year, 5, 2);

        /// <summary>
        /// Ende der Sommerzeit-Schaltung für die Heizung
        /// </summary>
        private static DateTime SummertimeEnd => new DateTime(DateTime.Now.Year, 10, 2);

        /// <summary>
        /// Niedrigste Temperatur draußen ab welcher die Heizung an gemacht wird
        /// </summary>
        private const double MinOutdoorTemperature = 10.0;

        /// <summary>
        /// Gibt an, ob die Heizungsregeln NICHT EINSCHALTEN,
        /// weil es Sommer ist (Einschalt-Regeln
        /// werden in diesem Fall einfach ignoriert)
        /// </summary>
        public static bool IsSummertimeOffDate(DateTime dt) => dt >= SummertimeStart && dt <= SummertimeEnd;

        /// <summary>
        /// Gibt an, ob es in den nächten 4 Stunden
        /// zu heiß ist, um die Heizung einzuschalten
        /// </summary>
        public static bool IsTooHot(IWeatherProvider weatherProvider)
        {
            if (weatherProvider.AverageTemperatureNext4Hours.HasValue)
            {
                return weatherProvider.AverageTemperatureNext4Hours >= MinOutdoorTemperature;
            } 
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ID (Autowert)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// ID des zugehörigen Raums in der Datenbank (Fremdschlüssel)
        /// </summary>
        [Column]
        [Required]
        public long? Id_Room { get; set; }

        /// <summary>
        /// Gibt an, ob diese Regel gerade aktiv ist
        /// </summary>
        /// <remarks>Default ist True</remarks>
        [Column]
        [Required]
        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Wochentage (Flags)
        /// </summary>
        [Column(TypeName = "int")]
        [EnumDataType(typeof(Weekdays))]
        [Display(Name = "Wochentage")]
        public Weekdays Weekday { get; set; }

        [NotMapped]
        [Display(Name = "Montags")]
        [Required]
        public bool IsMonday
        {
            get => Weekday.HasFlag(Weekdays.Montag);
            set  { if (value) Weekday |= Weekdays.Montag; else Weekday &= ~Weekdays.Montag; }
        }

        [NotMapped]
        [Required]
        [Display(Name = "Dienstags")]
        public bool IsTuesday
        {
            get => Weekday.HasFlag(Weekdays.Dienstag);
            set { if (value) Weekday |= Weekdays.Dienstag; else Weekday &= ~Weekdays.Dienstag; }
        }

        [NotMapped]
        [Display(Name = "Mittwochs")]
        [Required]
        public bool IsWednesday
        {
            get => Weekday.HasFlag(Weekdays.Mittwoch);
            set { if (value) Weekday |= Weekdays.Mittwoch; else Weekday &= ~Weekdays.Mittwoch; }
        }

        [NotMapped]
        [Display(Name = "Donnerstags")]
        [Required]
        public bool IsThursday
        {
            get => Weekday.HasFlag(Weekdays.Donnerstag);
            set { if (value) Weekday |= Weekdays.Donnerstag; else Weekday &= ~Weekdays.Donnerstag; }
        }

        [NotMapped]
        [Display(Name = "Freitags")]
        [Required]
        public bool IsFriday
        {
            get => Weekday.HasFlag(Weekdays.Freitag);
            set { if (value) Weekday |= Weekdays.Freitag; else Weekday &= ~Weekdays.Freitag; }
        }

        [NotMapped]
        [Display(Name = "Samstags")]
        [Required]
        public bool IsSaturday
        {
            get => Weekday.HasFlag(Weekdays.Samstag);
            set { if (value) Weekday |= Weekdays.Samstag; else Weekday &= ~Weekdays.Samstag; }
        }

        [NotMapped]
        [Display(Name = "Sonntags")]
        [Required]
        public bool IsSunday
        {
            get => Weekday.HasFlag(Weekdays.Sonntag);
            set { if (value) Weekday |= Weekdays.Sonntag; else Weekday &= ~Weekdays.Sonntag; }
        }


        /// <summary>
        /// Stunde (24h)
        /// </summary>
        [Column(TypeName = "int")]
        [Display(Name = "Stunde")]
        [Required]
        [Range(0, 23)]
        public byte Hour { get; set; }

        /// <summary>
        /// Minute
        /// </summary>
        [Display(Name = "Minute")]
        [Column(TypeName = "int")]
        [Required]
        [Range(0, 59)]
        public byte Minute { get; set; }

        /// <summary>
        /// Die Einzustellende Temperatur
        /// </summary>
        [Column(TypeName = "int")]
        [Display(Name = "Zieltemperatur")]
        [Required]
        [Range(0, 30)]
        public byte TemperatureSetpoint { get; set; }

        /// <summary>
        /// Formattierter Zeitpunkt dieser Regel für die Anzeige am UI
        /// </summary>
        [NotMapped]
        [Display(Name = "Zeitpunkt")]
        public string DisplayTime
        {
            get
            {

                string time = String.Format("{0} {1:00}:{2:00}", this.DisplayWeekday, this.Hour, this.Minute);

                return time;
            }
        }

        [NotMapped]
        [Display(Name = "Wochentage")]
        public string DisplayWeekday
        {
            get
            {
                string day;
                if (this.Weekday.HasFlag(Weekdays.Montag) &&
                    this.Weekday.HasFlag(Weekdays.Dienstag) &&
                    this.Weekday.HasFlag(Weekdays.Mittwoch) &&
                    this.Weekday.HasFlag(Weekdays.Donnerstag) &&
                    this.Weekday.HasFlag(Weekdays.Freitag) &&
                    this.Weekday.HasFlag(Weekdays.Samstag) &&
                    this.Weekday.HasFlag(Weekdays.Sonntag))
                {
                    day = "Jeden Tag";
                }
                else
                {
                    day = string.Join(", ", Enum.GetValues(typeof(Weekdays))
                        .Cast<Weekdays>()
                        .Where(d => this.Weekday.HasFlag(d))
                        .Select(d => d.ToString()));
                }
                return day;
            }
        }
    }
}
