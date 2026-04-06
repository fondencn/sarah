using System.Drawing;
using System.Linq;
using Sarah.API.Interfaces;
using Sarah.API.BusinessObjects;

namespace Sarah.Geofences
{
    public class GeoFence : IGeoFence
    {
        public required string Name { get; set; }
        public required LocatorPosition[] Points { get; set; }

        public static bool operator ==(GeoFence? left, GeoFence? right)
            => string.Equals(left?.Name, right?.Name, StringComparison.Ordinal);

        public static bool operator !=(GeoFence? left, GeoFence? right)
            => !string.Equals(left?.Name, right?.Name, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is GeoFence other && string.Equals(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

        public bool IsWithin(LocatorPosition pos) => IsPointInPolygon(this.Points.Select(item => (PointF) item).ToArray(), (PointF)pos);
        public bool IsWithin(LocationServiceEntry pos) => IsPointInPolygon(this.Points.Select(item => (PointF)item).ToArray(), (PointF)pos);

        private static bool IsPointInPolygon(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y ||
                    polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) /
                       (polygon[j].Y - polygon[i].Y) *
                       (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }
}
