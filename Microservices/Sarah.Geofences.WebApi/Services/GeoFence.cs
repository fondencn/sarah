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

        public bool IsWithin(LocatorPosition pos) => IsPointInPolygon(this.Points.Select(item => (PointF) item).ToArray(), (PointF)pos);
        public bool IsWithin(LocationServiceEntry pos) => IsPointInPolygon(this.Points.Select(item => (PointF)item).ToArray(), (PointF)pos);

        private static bool IsPointInPolygon(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.X && polygon[j].Y >= testPoint.X ||
                    polygon[j].Y < testPoint.X && polygon[i].Y >= testPoint.X)
                {
                    if (polygon[i].X + (testPoint.X - polygon[i].Y) /
                       (polygon[j].Y - polygon[i].Y) *
                       (polygon[j].X - polygon[i].X) < testPoint.Y)
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
