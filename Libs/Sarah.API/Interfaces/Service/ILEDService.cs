using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Service
{
    public interface ILEDService : IDisposable
    {
        Task Booting();

        Task Speaking();

        Task Thinking();

        Task Listening();

        Task FlashLight(bool isOn);
    }

    public class EmptyLEDService : ILEDService
    {
        public Task Booting() => Task.CompletedTask;

        public void Dispose()
        {
            /* nix zu tun */
        }

        public Task FlashLight(bool isOn) => Task.CompletedTask;

        public Task Listening() => Task.CompletedTask;

        public Task Speaking() => Task.CompletedTask;

        public Task Thinking() => Task.CompletedTask;
    }
}
