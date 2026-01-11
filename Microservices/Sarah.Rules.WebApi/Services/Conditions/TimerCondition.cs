using System;
using Sarah.API.BusinessObjects;

namespace Sarah.Rules.Conditions
{
    /// <summary>
    /// Bedingung, die überprüft, ob zum aktuellen Zeitpunkt 
    /// der angegebene Zeitpunkt erreicht ist. 
    /// </summary>
    public class TimerCondition : RuleCondition
    {
        public TimerRecurrence Recurrence { get; }

        public DateTime DateTime { get; }

        public bool IsOneShot { get; }

        public TimerCondition(TimerRecurrence recurrence) : this(recurrence, 0)
        {
        }

        public TimerCondition(TimerRecurrence recurrence, byte targetNodeid) : base(targetNodeid)
        {
            if (recurrence == null)
            {
                throw new ArgumentNullException(nameof(recurrence));
            }
            this.IsOneShot = false;
            this.Recurrence = recurrence;
        }


        public TimerCondition(DateTime alarmTime) : base(0)
        {
            this.IsOneShot = true;
            this.DateTime = alarmTime;
        }

        /// <summary>
        /// Wertet die aktuelle Systemzeit aus und gibt true zurück, wenn die Eingestellte Timer-Bedingung zutrifft. 
        /// </summary>
        /// <returns></returns>
        public override bool Evaluate(NetworkEvent evt)
        {
            if (IsOneShot)
            {
                int reminderAccuracy = 10; //auf 10 sekunden genau
                return evt.SourceNodeId == this.TargetNodeId && Math.Abs( (this.DateTime - DateTime.Now).TotalSeconds ) < reminderAccuracy;
            }
            else
            {
                return evt.SourceNodeId == this.TargetNodeId && Recurrence.Matches(DateTime.Now);
            }
        }
    }
}
