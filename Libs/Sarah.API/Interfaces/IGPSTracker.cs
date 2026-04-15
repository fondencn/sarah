using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using System;

namespace Sarah.API.Interfaces
{
    public interface IGPSTracker :  INetworkElement
    {
        SensorData? Battery { get; }
        LocatorPosition Position { get; }
        LocatorPosition[] PositionTrace { get; }
        DateTime LastMessageReceived { get; }
        SensorData IsButtonPressed { get; }
    }
}
