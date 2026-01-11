using Sarah.API.Business;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface ISmokeSensor : INetworkElement
    {
        SensorData IsSmokeDetected { get; }
        SensorData IsOverheatingDetected { get; }
        SensorData Alarm { get; }
    }
}
