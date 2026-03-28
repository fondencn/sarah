using Sarah.API.Interfaces.Service;
using Sarah.LEDService;

namespace Sarah.SpeechServer;

public static class LedServiceFactory
{
    public static ILEDService CreateLedService(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<ReSpeakerLEDService>>();

        try
        {
            return new ReSpeakerLEDService(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize ReSpeakerLEDService. Falling back to EmptyLEDService.");
            logger.LogInformation("If you are running on a ReSpeaker device, please ensure that the necessary hardware and drivers are properly set up.");
            return new EmptyLEDService();
        }
    }
}