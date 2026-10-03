using System;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IWallPlug : INetworkElement
    {
        bool IsOn { get; }
        DateTime LastChangeToPowerLow { get; }
        DateTime LastChangeToPowerHigh { get; }

        Task SetState(bool newValue);
        void ToggleState();
    }
}
