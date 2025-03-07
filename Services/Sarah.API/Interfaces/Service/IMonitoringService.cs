using System.Threading;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects;


namespace Sarah.API.Interfaces.Services
{
    public interface IMonitoringService
    {

        Task Start();

        IWeatherProvider Weather {get; }
        IFerienInfoProvider Ferien {get; }
    }
}