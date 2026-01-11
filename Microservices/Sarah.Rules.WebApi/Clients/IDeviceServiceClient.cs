using Sarah.Rules.DTOs.DeviceCommands;

namespace Sarah.Rules.Clients
{
    public interface IDeviceServiceClient
    {
        Task ExecuteBlinkAnimationAsync(BlinkAnimationCommand command);
        Task StartSceneAsync(StartSceneCommand command);
        Task StopSceneAsync(StopSceneCommand command);
    }
}
