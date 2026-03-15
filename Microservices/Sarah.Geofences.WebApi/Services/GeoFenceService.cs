using System;
using System.Collections.Generic;
using System.Linq;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;

namespace Sarah.Geofences
{
    public class GeoFenceService : IGeoFenceService
    {
        public static readonly GeoFence Zuhause = new GeoFence()
        {
            Name = "Zuhause",
            Points = new LocatorPosition[]
            { //, 
                     new LocatorPosition(new API.Business.SensorData(48.888263f, "°"), new API.Business.SensorData(9.209489f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.888093f, "°"), new API.Business.SensorData(9.210949f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.887734f, "°"), new API.Business.SensorData(9.210869f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.887424f, "°"), new API.Business.SensorData(9.210824f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.887569f, "°"), new API.Business.SensorData(9.209291f, "°")),
            }
        };
        public static readonly GeoFence OHG = new GeoFence()
        {
            Name = "OHG",
            Points = new LocatorPosition[]
            {
                    //48.899452, 9.172917
                    //48.900157, 9.178509
                    //48.898377, 9.179099
                    //48.897678, 9.173517
                     new LocatorPosition(new API.Business.SensorData(48.899452f, "°"), new API.Business.SensorData(9.172917f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.900157f, "°"), new API.Business.SensorData(9.178509f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.898377f, "°"), new API.Business.SensorData(9.179099f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.897678f, "°"), new API.Business.SensorData(9.173517f, "°")),
            }
        };
        public static readonly GeoFence Sentris = new GeoFence()
        {
            Name = "Sentris Osterholzallee",
            Points = new LocatorPosition[]
            {
                     new LocatorPosition(new API.Business.SensorData(48.897007f, "°"), new API.Business.SensorData(9.162510f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.898117f, "°"), new API.Business.SensorData(9.161418f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.898318f, "°"), new API.Business.SensorData(9.165320f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.897064f, "°"), new API.Business.SensorData(9.165352f, "°")),
            }
        };
        public static readonly GeoFence SentrisKammererstr = new GeoFence()
        {
            Name = "Sentris Kammererstraße",
            Points = new LocatorPosition[]
            {
                     new LocatorPosition(new API.Business.SensorData(48.8875598f, "°"), new API.Business.SensorData(9.1757709f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.8881260f, "°"), new API.Business.SensorData(9.1770597f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.8868791f, "°"), new API.Business.SensorData(9.1777597f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.8866974f, "°"), new API.Business.SensorData(9.1759117f, "°")),
            }
        };

        public static readonly GeoFence Henrik = new GeoFence()
        {
            Name = "Henrik",
            Points = new LocatorPosition[]
            {
                     new LocatorPosition(new API.Business.SensorData(48.883473f, "°"), new API.Business.SensorData(9.214499f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.883491f, "°"), new API.Business.SensorData(9.214832f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.883801f, "°"), new API.Business.SensorData(9.214773f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.883803f, "°"), new API.Business.SensorData(9.214478f, "°")),
            }
        };

        public static readonly GeoFence Karate = new GeoFence()
        {
            Name = "Karate",
            Points = new LocatorPosition[]
            {
                
                     //48.890257, 9.214918
                     //48.890236, 9.215554
                     //48.889855, 9.215466
                     //48.889833, 9.214796
                     new LocatorPosition(new API.Business.SensorData(48.890257f, "°"), new API.Business.SensorData(9.214918f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.890236f, "°"), new API.Business.SensorData(9.215554f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.889855f, "°"), new API.Business.SensorData(9.215466f, "°")),
                     new LocatorPosition(new API.Business.SensorData(48.889833f, "°"), new API.Business.SensorData(9.214796f, "°")),
            }
        };

        public static readonly GeoFence Mannheim = new GeoFence()
        {
            Name = "Mannheim + Ludwigshafen + Viernheim",
            Points = new LocatorPosition[]
            {
                     // Large urban boundary including Mannheim, Ludwigshafen and Viernheim
                     new LocatorPosition(new API.Business.SensorData(49.600000f, "°"), new API.Business.SensorData(8.350000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.620000f, "°"), new API.Business.SensorData(8.520000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.580000f, "°"), new API.Business.SensorData(8.610000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.500000f, "°"), new API.Business.SensorData(8.630000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.420000f, "°"), new API.Business.SensorData(8.610000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.390000f, "°"), new API.Business.SensorData(8.520000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.410000f, "°"), new API.Business.SensorData(8.420000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.470000f, "°"), new API.Business.SensorData(8.360000f, "°")),
            }
        };

        public static readonly GeoFence Schwetzingen = new GeoFence()
        {
            Name = "Schwetzingen",
            Points = new LocatorPosition[]
            {
                     new LocatorPosition(new API.Business.SensorData(49.430000f, "°"), new API.Business.SensorData(8.500000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.425000f, "°"), new API.Business.SensorData(8.615000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.335000f, "°"), new API.Business.SensorData(8.620000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.325000f, "°"), new API.Business.SensorData(8.500000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.370000f, "°"), new API.Business.SensorData(8.470000f, "°")),
            }
        };

        public static readonly GeoFence Speyer = new GeoFence()
        {
            Name = "Speyer",
            Points = new LocatorPosition[]
            {
                     new LocatorPosition(new API.Business.SensorData(49.350000f, "°"), new API.Business.SensorData(8.370000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.345000f, "°"), new API.Business.SensorData(8.500000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.250000f, "°"), new API.Business.SensorData(8.505000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.245000f, "°"), new API.Business.SensorData(8.365000f, "°")),
            }
        };

        public static readonly GeoFence FuerthNuremberg = new GeoFence()
        {
            Name = "Nürnberg/Fürth",
            Points = new LocatorPosition[]
            {
                     // Urban area covering Fuerth and Nuremberg
                     new LocatorPosition(new API.Business.SensorData(49.525000f, "°"), new API.Business.SensorData(10.930000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.510000f, "°"), new API.Business.SensorData(11.180000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.390000f, "°"), new API.Business.SensorData(11.180000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.360000f, "°"), new API.Business.SensorData(10.960000f, "°")),
                     new LocatorPosition(new API.Business.SensorData(49.430000f, "°"), new API.Business.SensorData(10.900000f, "°")),
            }
        };


        public static IEnumerable<GeoFence> All
        {
            get
            {
                yield return OHG;
                yield return Zuhause;
                yield return Sentris;
                yield return SentrisKammererstr;
                yield return Henrik;
                yield return Karate;
                yield return Mannheim;
                yield return Schwetzingen;
                yield return Speyer;
                yield return FuerthNuremberg;
            }
        }

        // public static GeoFence GetCurrent(LocationServiceEntry pos)
        //     => All.FirstOrDefault(item => item.IsWithin(pos));

        public IGeoFence? GetCurrent(LocatorPosition? pos)
            => pos == null ? null : All.FirstOrDefault(item => item.IsWithin(pos));

        public IGeoFence GetZuhause() => Zuhause;

        public IEnumerable<IGeoFence> GetAll() => All;
    }
}
