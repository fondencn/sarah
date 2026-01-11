using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface INode
    {
        Task<string> GetDeviceTypeName();
    }
}
