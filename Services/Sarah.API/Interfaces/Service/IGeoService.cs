using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IGeoService
    {
        Task<IRouteInfo> GetRouteInfo(string from, string to, TravelType travelType = TravelType.Driving);
        Task<IRouteInfo> GetRouteInfoAzure(string from, string to, TravelType travelType = TravelType.Driving);
    }

    public interface IRouteInfo
    {
        TimeSpan Duration { get; }
        string From { get; }
        string To { get; }
        double Distance { get; }
        string TrafficCongestion { get; }
    }

    public enum TravelType
    {
        Driving,
        Walking,
        Transit,
        Truck, 
        Bicycle
    }
}
