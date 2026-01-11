using System;
using System.Drawing;

namespace Sarah.API.BusinessObjects
{
    public class ProtectedLocationServiceEntry : LocationServiceEntry
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class LocationServiceEntry
    {
        public string PersonName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longtitude { get; set; }
        public double Battery { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }


        public static explicit operator PointF(LocationServiceEntry pos) =>
            new PointF((float)pos.Longtitude, (float)pos.Latitude);
    }
}
