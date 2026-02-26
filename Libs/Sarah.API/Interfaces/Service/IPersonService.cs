using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Services
{
    public interface IPersonService
    {
        Task AddPersonAsync(IPerson person);
        Task DeletePersonAsync(long id);
        Task<IEnumerable<IPerson>> GetAllPersonsAsync();
        Task<IPerson?> GetPersonByIdAsync(long id);
        Task UpdatePersonAsync(IPerson person);
        Task<string[]> GetMobilePhones();
        Task<string[]> GetKnownHomeNetworkDevices();
        Task<bool> IsSomeonePresent();
    }
}