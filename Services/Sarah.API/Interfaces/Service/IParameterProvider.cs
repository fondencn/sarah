using Sarah.API.BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IParameterProvider
    {
        Task<IEnumerable<DeviceParameter>> GetParameters(byte nodeId);
        Task SetParameter(byte nodeId, DeviceParameter p);
        Task<DeviceParameter> GetParameter(byte nodeID, byte paramId);
    }
}
