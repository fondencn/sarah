using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;

namespace Sarah.API.Interfaces.Services
{
    public interface IGeoFenceService
    {
        IGeoFence? GetCurrent(LocatorPosition pos);
        IGeoFence GetZuhause();
    }
}