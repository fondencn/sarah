using System;
using Sarah.API.Extensions;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Timer-Wiederkehr
    /// </summary>
    public class TimerRecurrence
    {
        /// <summary>
        /// Stunde 0-24
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Minute 0-60
        /// </summary>
        public int Minute { get; set; }

        /// <summary>
        /// Wochentage (Flags)
        /// </summary>
        public Weekdays Weekdays { get; set; }

        /// <summary>
        /// Wiederholungsintervall
        /// </summary>
        public RecurrenceInterval Interval { get; set; } = RecurrenceInterval.Täglich;

        /// <summary>
        /// Startdatum der Wiederholung
        /// </summary>
        public DateTime? From { get; set; }

        /// <summary>
        /// Enddatum der Wiederholung
        /// </summary>
        public DateTime? Until { get; set; }


        /// <summary>
        /// Gibt an, ob diese Recurrence zum angegebenen Zeitpunkt zutrifft oder nicht
        /// </summary>
        /// <param name="dt">Messzeitpunkt</param>
        /// <returns>true wenn diese Recurrence zutrifft, sonst false</returns>
        public bool Matches(DateTime dt)
        {
            bool isMatch = Hour >= 0 || Minute >= 0;
            if (Hour >= 0)
            {
                isMatch &= Hour == dt.Hour;
            }
            if (Minute >= 0)
            {
                isMatch &= Minute == dt.Minute;
            }
            if (Weekdays != Weekdays.Unknown)
            {
                isMatch &= (Weekdays.HasFlag(Weekdays.Montag) && dt.DayOfWeek == DayOfWeek.Monday
                    || Weekdays.HasFlag(Weekdays.Dienstag) && dt.DayOfWeek == DayOfWeek.Tuesday
                    || Weekdays.HasFlag(Weekdays.Mittwoch) && dt.DayOfWeek == DayOfWeek.Wednesday
                    || Weekdays.HasFlag(Weekdays.Donnerstag) && dt.DayOfWeek == DayOfWeek.Thursday
                    || Weekdays.HasFlag(Weekdays.Freitag) && dt.DayOfWeek == DayOfWeek.Friday
                    || Weekdays.HasFlag(Weekdays.Samstag) && dt.DayOfWeek == DayOfWeek.Saturday
                    || Weekdays.HasFlag(Weekdays.Sonntag) && dt.DayOfWeek == DayOfWeek.Sunday
                    );
            }
            return isMatch;
        }

        /// <summary>
        /// Erzeugt eine täglich auftretende Wiederholung
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public static TimerRecurrence EveryDay(int hour, int minute)
        {
            return new TimerRecurrence()
            {
                Weekdays =
                    Weekdays.Montag |
                    Weekdays.Dienstag |
                    Weekdays.Mittwoch |
                    Weekdays.Donnerstag |
                    Weekdays.Freitag |
                    Weekdays.Samstag |
                    Weekdays.Sonntag,
                Hour = hour,
                Minute = minute
            };
        }

        /// <summary>
        /// Ermittelt das nächste konkrete Auftreten dieses Zeitereignisses
        /// </summary>
        /// <param name="lastOccurence">Das letzte Eintreten des Ereignisses oder NULL falls es noch nie eingetreten ist</param>
        /// <returns>Nächstes Eintreten in der Zukunft als DateTime</returns>
        public DateTime GetNext(DateTime? lastOccurence = null)
        {
            DateTime today = DateTime.Today;
            DateTime next;
            if (lastOccurence.HasValue)
            {
                next = lastOccurence.Value;
            }
            else if (this.From.HasValue)
            {
                next = this.From.Value;
            }
            else
            {
                next = today.AddHours(this.Hour).AddMinutes(this.Minute);
            }

            /* Nächsten passenden Wochentag finden, damit wir einen Aufsetzpunkt haben */
            while(!next.IsWeekdayMatching(this.Weekdays))
            {
                /* Rückwärts gehen bis zum 1. passenden Wochentag */
                next = next.AddDays(-1);
            }

            while (!next.IsInFuture() || !next.IsWeekdayMatching(this.Weekdays))
            {
                next = next.GetNextOccurrence(this.Interval);
            }


            //List<int> daysUntilWeekdays = GetDaysUntilNextMatchingWeekday();
            //// TODO: Falsch wenn der Tag heute und bereits vorbei ist

            //IEnumerable<DateTime> dates = daysUntilWeekdays.Select(day => today.AddDays(day).AddHours(this.Hour).AddMinutes(this.Minute));
            //List<DateTime> datesNormalized = new List<DateTime>();
            //foreach (DateTime date in dates)
            //{
            //    bool isInPast = (DateTime.Now - date).TotalMilliseconds > 0;
            //    if (isInPast)
            //    {
            //        datesNormalized.Add(date.AddDays(7)); //dann eine Woche später
            //    }
            //    else
            //    {
            //        datesNormalized.Add(date);
            //    }
            //}

            //next = datesNormalized.Min();


            return next;
        }
    }
}
