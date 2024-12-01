using Sarah.API.Business;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IThermoElement : INetworkElement
    {
        Task SetLevel(byte newValue);
        Task SetTemperature(float newValue);

        SensorData TemperatureSetpoint { get; }
    }
}
