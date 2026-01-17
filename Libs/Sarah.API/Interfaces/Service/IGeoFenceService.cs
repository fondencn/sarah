using System.Collections.Generic;
using Sarah.API.BusinessObjects;

namespace Sarah.API.Interfaces.Services
{
    public interface IGeoFenceService
    {
        IGeoFence? GetCurrent(LocatorPosition pos);
        IGeoFence GetZuhause();
        IEnumerable<IGeoFence> GetAll();
    }
}