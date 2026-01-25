using Sarah.API.Business;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Positionsdaten eines Trackerobjektes (Geokoordinaten) 
    /// </summary>
    public class LocatorPosition : IEquatable<LocatorPosition>
    {
        /// <summary>
        /// Längengrad
        /// </summary>
        public SensorData Longtitude { get; private set; }
        /// <summary>
        /// Breitengrad
        /// </summary>
        public SensorData Latitude { get; private set; }

        /// <summary>
        /// Zeitpunkt, zu welchem die Positionsdaten ermittelt wurden
        /// </summary>
        public DateTime MeasureTime { get; private set; }

        /// <summary>
        /// Gibt an, ob es sich um eine gültige Koordinate handelt
        /// </summary>
        public bool IsValid => Longtitude.Value != 0
            && Latitude.Value != 0;

        /// <summary>
        /// Zu Hause
        ///  var zuhause_lat = 48.8878487;
        ///  var zuhause_lon = 9.209924;
        /// </summary>
        public static LocatorPosition ZuHause { get; }
            = new LocatorPosition(new SensorData(48.8878487f, "°"), new SensorData(9.209924f, "°"));
        public static LocatorPosition Empty { get; } = new LocatorPosition(new SensorData(0.0f, "°"), new SensorData(0.0f, "°"));

        /// <summary>
        /// ctor
        /// </summary>
        public LocatorPosition(SensorData lon, SensorData lat)
        {
            this.Longtitude = lon;
            this.Latitude = lat;
            this.MeasureTime = DateTime.Now;
        }


        /// <summary>
        /// Stringdarstellung
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} | {1}", Latitude?.ToString() ?? String.Empty , Longtitude?.ToString() ?? String.Empty);
        }

        /// <summary>
        /// Berechnet den Abstand zwischen diesem Geopunkt und dem übergebenen Geopunkt
        /// </summary>
        /// <param name="other">Anderer Geopunkt, zu den der ABstand berechnet werden soll</param>
        /// <returns>Abstand in Meter oder 0, falls einer der beiden Punkte keine gültige Koordinate besitzt.</returns>
        public double GetDistanceTo(LocatorPosition other)
        {
            if (!this.IsValid) 
            {
                return 0;
            }
            if (!other.IsValid)
            {
                return 0;
            }

            const double R = 6371; // Radius der Erde in Kilometern
            var lat1Rad = DegreesToRadians(this.Longtitude.Value); // Achtung: Long und Lat vertauscht!!
            var lon1Rad = DegreesToRadians(this.Latitude.Value);
            var lat2Rad = DegreesToRadians(other.Latitude.Value);
            var lon2Rad = DegreesToRadians(other.Longtitude.Value);

            var dLat = lat2Rad - lat1Rad;
            var dLon = lon2Rad - lon1Rad;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                     Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                     Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }



    /// <summary>
    /// Equals / by sensor value
    /// </summary>
    /// <param name="other">other sensor data</param>
    /// <returns>true if the value property of both items are equal</returns>
    public bool Equals(LocatorPosition? other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            else
            {
                return this.ToString().Equals(other.ToString());
            }
        }

        public override bool Equals(object obj)
        {
            LocatorPosition? other = obj as LocatorPosition;
            return other?.Equals(this) == true;
        }

        public static bool operator ==(LocatorPosition? lhs, LocatorPosition? rhs)
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
                return lhs.ToString().Equals(rhs.ToString());
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(LocatorPosition? lhs, LocatorPosition? rhs)
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
            else if (!object.ReferenceEquals(lhs, null) && !object.ReferenceEquals(rhs, null))
            {
                return !lhs.ToString().Equals(rhs.ToString());
            }
            else
            {
                return true;
            }
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static explicit operator PointF(LocatorPosition pos) => 
            new PointF(pos.Longtitude.Value, pos.Latitude.Value);
    }
}
