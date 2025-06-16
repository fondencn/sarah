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
        speechService.Initialize(location, true, true);
    }
    public static Task UseSpeechEvents(this WebApplication app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        var subscriber = app.Services.GetRequiredService<SpeechEventSubscriber>();
        return subscriber.Initialize();
    }
}
