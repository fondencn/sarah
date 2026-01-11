using System;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IWallPlug : INetworkElement
    {
        bool IsOn { get; }
        DateTime LastChangeToPowerLow { get; }
        DateTime LastIncreasePower { get; }
        DateTime LastDecreasePower { get; }

        Task SetState(bool newValue);
        void ToggleState();
    }
}
