using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Service
{
    public interface IPersonService
    {
        Task<IEnumerable<IPerson>> GetAllPersonsAsync();
    }
}