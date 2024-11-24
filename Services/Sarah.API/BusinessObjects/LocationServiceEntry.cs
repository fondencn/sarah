using System;
using System.Drawing;

namespace Sarah.API.BusinessObjects
{
    public class ProtectedLocationServiceEntry : LocationServiceEntry
    {
        public string ApiKey { get; set; }
    }

    public class LocationServiceEntry
    {
        public string PersonName { get; set; }
        public double Latitude { get; set; }
        public double Longtitude { get; set; }
        public double Battery { get; set; }
        public string DeviceName { get; set; }
        public DateTime DateTime { get; set; }


        public static explicit operator PointF(LocationServiceEntry pos) =>
            new PointF((float)pos.Longtitude, (float)pos.Latitude);
    }
}
