using Sarah.API.Business;

namespace Sarah.API.Interfaces
{
    public interface IMultiSensor : INetworkElement
    {
        SensorData Luminance { get; }
        SensorData Presence { get; }
        SensorData RelativeHumidity { get; }
        SensorData VolatileOrganicCompounds { get; }
        SensorData CO2 { get; }
    }
}
