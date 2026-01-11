using Sarah.API.BusinessObjects;
using System;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface ILamp : INetworkElement
    {
        DateTime? LastChange { get; }
        byte Brightness { get; }
        string Color { get; }

        Task SetBrightness(byte brightness);
        Task SetColor(string color);
        Task SetWarmWhite();
        Task ToggleState();
        Task SetColdWhite();
        Animation? CurrentAnimation { get; set; }
    }
}
