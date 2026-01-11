using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sarah.Data.Models;

namespace Sarah.Data.Attributes
{
    public class DateInFutureAttribute : ValidationAttribute
    {
        public override bool IsValid(object? input)
        {
            DateTime date = input == null ? DateTime.MinValue : (DateTime)input;
            return date > DateTime.Now;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Das Datum muss in der Zukunft liegen";
        }
    }

    public class DateInFutureOrRecurrenceAttribute : ValidationAttribute
    {
        public override bool IsValid(object? input)
        {
            AlarmSchedule? item = input as AlarmSchedule;
            if (item != null)
            {
                return item.HasRecurrence || item.AlarmTime > DateTime.Now;
            }
            else
            {
                /* Attribut darf nur an Klassen vom Typ AlarmSchedule drangemacht werden */
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return "Es muss sich um einen Serientermin handeln oder das Datum muss in der Zukunft liegen";
        }
    }

    /// <summary>
    /// Überprüft, ob im angegebenen Objekt das FromDate vor dem ToDate ist, sofern beide gesetzt sind.
    /// </summary>
    public class DateRangeValid : ValidationAttribute
    {
        private readonly string _fromProp;
        private readonly string _toProp;

        public DateRangeValid(string fromProp, string toProp) : base()
        {
            this._fromProp = fromProp;
            this._toProp = toProp;
        }

        public override bool IsValid(object? input)
        {
            AlarmScheduleRecurrence? item = input == null ? null :  input as AlarmScheduleRecurrence;
            if (item != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                DateTime? fromDate = typeof(AlarmScheduleRecurrence).GetProperty(_fromProp).GetValue(item) as DateTime?;
                DateTime? toDate = typeof(AlarmScheduleRecurrence).GetProperty(_toProp).GetValue(item) as DateTime?;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                if (fromDate.HasValue && toDate.HasValue)
                {
                    return fromDate.Value < toDate.Value;
                }
                else
                {
                    /* nichts ist auch ok */
                    return true;
                }
            }
            else
            {
                /* Attribut darf nur an Klassen vom Typ AlarmScheduleRecurrence drangemacht werden */
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return "Das Startdatum muss vor dem Enddatum liegen.";
        }
    }
}
