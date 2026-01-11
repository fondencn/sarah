using System;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;

namespace Sarah.SpeechServer;

public class SpeechEventSubscriber(IEventProcessingService events, ISpeechService speech) : ISpeechEventSubscriber
{
    public Task SubscribeToEvents()
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        if (speech == null) throw new ArgumentNullException(nameof(speech));

        return events.SubscribeSpeechEventAsync(this);
    }


    public Task Initialize()
    {
        return SubscribeToEvents();
    }

    public Task Notify(SayEvent e)
    {
        speech.SayWithVolume(e.Message, e.Volume);
        return Task.CompletedTask;
    }
}
