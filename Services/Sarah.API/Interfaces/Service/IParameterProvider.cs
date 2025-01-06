using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IParameterProvider
    {
        Task<IEnumerable<DeviceParameter>> GetParameters(byte nodeId, IDeviceService deviceService);
        Task SetParameter(byte nodeId, DeviceParameter p, IDeviceService deviceService);
        Task<DeviceParameter> GetParameter(byte nodeID, byte paramId, IDeviceService deviceService);
    }
}
