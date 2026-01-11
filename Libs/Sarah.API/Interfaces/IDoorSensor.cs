using System;
using Sarah.API.BusinessObjects;

namespace Sarah.API.Interfaces
{
    public interface IDoorSensor : INetworkElement
    {
        DoorSensorState State { get; }
        DateTime? LastStateChanged { get; }
        TimeSpan? LastOpenDuration { get; }
    }
}
