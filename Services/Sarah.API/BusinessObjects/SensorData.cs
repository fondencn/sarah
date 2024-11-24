using System;
using System.Globalization;

namespace Sarah.API.Business

{
    /// <summary>
    /// Sensordaten mit einem Wert, einer Einheit und dem Erfassungsdatum
    /// </summary>
    public class SensorData : IEquatable<SensorData>
    {
        /// <summary>
        /// Sensorwert
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Einheit des Sensorwertes
        /// </summary>
        public string Unit { get; set; } = "?";

        /// <summary>
        /// Letzter Erfassungszeitpunkt des Wertes
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="value">Wert</param>
        /// <param name="unit">Einheit</param>
        public SensorData(float value, string unit)
        {
            this.Value = value;
            this.Unit = unit;
            this.Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Stringdarstellung
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format (CultureInfo.CurrentCulture, "{0} {1}", Value, Unit ?? String.Empty);
        }

        /// <summary>
        /// Equals / by sensor value
        /// </summary>
        /// <param name="other">other sensor data</param>
        /// <returns>true if the value property of both items are equal</returns>
        public bool Equals(SensorData other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            else
            {
                return this.Value.Equals(other.Value);
            }
        }

        public override bool Equals(object obj)
        {
            SensorData other = obj as SensorData;
            return other?.Equals(this) == true;
        }

        public static bool operator ==(SensorData lhs, SensorData rhs)
        {
            if (object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null))
            {
                return true;
            }
            else if (object.ReferenceEquals(lhs, null) && !object.ReferenceEquals(rhs, null))
            {
                return false;
            }
            else if (!object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null))
            {
                return false;
            }
            else if (!object.ReferenceEquals(lhs, null) && !object.ReferenceEquals(rhs, null))
            {
                return lhs.Value.Equals(rhs.Value);
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(SensorData lhs, SensorData rhs)
        {
            if (object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null))
            {
                return false;
            }
            else if (object.ReferenceEquals(lhs, null) && !object.ReferenceEquals(rhs, null))
            {
                return true;
            }
            else if (!object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null))
            {
                return true;
            }
            else if(!object.ReferenceEquals(lhs, null) && !object.ReferenceEquals(rhs, null))
            {
                return !lhs.Value.Equals(rhs.Value);
            }
            else
            {
                return true;
            }
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }
    }
}
