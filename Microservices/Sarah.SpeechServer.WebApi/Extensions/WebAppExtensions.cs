using System;
using Sarah.API.Interfaces;

namespace Sarah.SpeechServer.Extensions;

public static class WebAppExtensions
{
    public static void UseSpeechService(this WebApplication app, IConfiguration config)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        var speechService = app.Services.GetRequiredService<ISpeechService>();
        var location = config["Location"] ?? "default-location";
        var recognitionEnabled = !string.Equals(config["Speech:RecognitionEnabled"], "false", StringComparison.OrdinalIgnoreCase);
        speechService.Initialize(location, recognitionEnabled, true);
    }
}
