using Sarah.API.Business;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface ITemperatureSensor
    {
        SensorData Temperature { get; }
    }
}
