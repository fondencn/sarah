using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Services
{
    public interface IPersonService
    {
        Task AddPersonAsync(IPerson person);
        Task DeletePersonAsync(int id);
        Task<IEnumerable<IPerson>> GetAllPersonsAsync();
        Task<IPerson> GetPersonByIdAsync(int id);
        Task UpdatePersonAsync(IPerson person);
    }
}