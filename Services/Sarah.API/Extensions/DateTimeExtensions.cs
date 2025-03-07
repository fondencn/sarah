using System;
using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;

namespace Sarah.API.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Gibt an, ob das Datum in der in der Ruhezeit liegt
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool IsInSilentTime(this DateTime dte, IConfiguration config) 
        {
            int silentStart = int.Parse(config["SilentStartHour"] ?? "22");
            int silentEnd = int.Parse(config["SilentStartHour"] ?? "6");

            if (silentStart == 0 && silentEnd == 0)
            {
                return false;
            }

            var silentStartDateTime = new DateTime(dte.Year, dte.Month, dte.Day, silentStart, 0, 0);
            var silentEndDateTime = new DateTime(dte.Year, dte.Month, dte.Day, silentEnd, 0, 0);

            if (silentStartDateTime > silentEndDateTime)
            {
                silentEndDateTime = silentEndDateTime.AddDays(1);
            }

            return dte >= silentStartDateTime && dte <= silentEndDateTime;     
        }


        /// <summary>
        /// Gibt an, ob sich das Datum in der Zukunft befindet mindestens einige Sekunden, weicher Vergleich)
        /// </summary>
        /// <param name="dte"></param>
        /// <returns></returns>
        public static bool IsInFuture(this DateTime dte)
        {
            return dte > DateTime.Now.AddSeconds(2);
        }

        /// <summary>
        /// Gibt an, ob der Wochentag des Datums mit der (eigenen) Enum Weekdays übereinstimmt (Flag-Matching)
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="weekdays"></param>
        /// <returns></returns>
        public static bool IsWeekdayMatching(this DateTime dte, Weekdays weekdays)
        {
            return weekdays == Weekdays.Unknown //jeder Tag
                || (dte.DayOfWeek == DayOfWeek.Monday && weekdays.HasFlag(Weekdays.Montag))
                || (dte.DayOfWeek == DayOfWeek.Tuesday && weekdays.HasFlag(Weekdays.Dienstag))
                || (dte.DayOfWeek == DayOfWeek.Wednesday && weekdays.HasFlag(Weekdays.Mittwoch))
                || (dte.DayOfWeek == DayOfWeek.Thursday && weekdays.HasFlag(Weekdays.Donnerstag))
                || (dte.DayOfWeek == DayOfWeek.Friday && weekdays.HasFlag(Weekdays.Freitag))
                || (dte.DayOfWeek == DayOfWeek.Saturday && weekdays.HasFlag(Weekdays.Samstag))
                || (dte.DayOfWeek == DayOfWeek.Sunday && weekdays.HasFlag(Weekdays.Sonntag));
        }


        /// <summary>
        /// Ermittelt das Folgedatum zu diesem Datum basierend auf dem angegebenen Intervall
        /// </summary>
        /// <param name="next"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static DateTime GetNextOccurrence(this DateTime next, RecurrenceInterval interval)
        {
            if (interval == RecurrenceInterval.Täglich)
            {
                return next.AddDays(1);
            }
            else if (interval == RecurrenceInterval.Wöchentlich)
            {
                return next.AddDays(7);
            }
            else if (interval == RecurrenceInterval.Monatlich)
            {
                return next.AddMonths(1);
            }
            else if (interval == RecurrenceInterval.Jährlich)
            {
                return next.AddYears(1);
            }
            else
            {
                throw new NotSupportedException("Das angegebene RecurrenceInterval wird nicht unterstützt.");
            }
        }
    }
}