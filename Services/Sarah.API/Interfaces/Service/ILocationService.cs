using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationServiceEntry>> GetLocationTrace(string personName, string apiKey);
        Task<LocationServiceEntry?> GetLocation(string personName, string apiKey);
        Task AddLocation(ProtectedLocationServiceEntry item);
    }
}
