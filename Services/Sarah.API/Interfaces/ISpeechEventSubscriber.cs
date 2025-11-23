using Sarah.API.BusinessObjects;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface ISpeechEventSubscriber
    {
        Task Notify(SayEvent e);
    }
}
