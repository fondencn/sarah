using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces.Service;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public static class IntentFactory
    {
        public static List<Intent> CreateIntents(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILEDService ledService, ILoggerFactory loggerFactory)
        {
            if (speechService is null)
            {
                throw new ArgumentNullException(nameof(speechService));
            }

            List<Intent> intents = new List<Intent>();
            CreateChattyIntents(speechService, intents, loggerFactory);
            intents.Add(new SwitchControlIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<SwitchControlIntent>()));
            intents.Add(new FlashlightIntent(speechService, deviceServiceClient, ledService, loggerFactory.CreateLogger<FlashlightIntent>()));
            intents.Add(new HeatingControlIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<HeatingControlIntent>()));
            intents.Add(new CreateAlarmIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<CreateAlarmIntent>()));
            intents.Add(new GetAlarmsIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<GetAlarmsIntent>()));
            intents.Add(new JokeIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<JokeIntent>()));
            intents.Add(new OpenDoorIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<OpenDoorIntent>()));
            intents.Add(new SceneIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<SceneIntent>()));
            intents.Add(new ClockIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<ClockIntent>()));
            intents.Add(new WeatherIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<WeatherIntent>()));
            intents.Add(new CoronaIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<CoronaIntent>()));
            intents.Add(new SilentIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<SilentIntent>()));
            intents.Add(new FindPersonIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<FindPersonIntent>()));
            intents.Add(new HelpIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<HelpIntent>()));
            /* den NoMatchIntent als letztes, das ist der catch-all */
            intents.Add(new NoMatchIntent(speechService, deviceServiceClient, loggerFactory.CreateLogger<NoMatchIntent>()));


            return intents;
        }

        /// <summary>
        /// Ein- und Ausgabe für  legere Unterhaltungen
        /// </summary>
        /// <param name="speechService"></param>
        /// <param name="intents"></param>
        private static void CreateChattyIntents(SpeechService speechService, List<Intent> intents, ILoggerFactory loggerFactory)
        {
            intents.Add(new DialogIntent(speechService, null, loggerFactory.CreateLogger<DialogIntent>(), "Wer bist du|[Wie|Was] ist dein Name|Wie heißt du", "Ich bin Sarah, eine künstliche Intelligenz.", "Mein Name ist Sarah", "Ich bin Sarah, ich kann dir helfen"));
            intents.Add(new DialogIntent(speechService, null, loggerFactory.CreateLogger<DialogIntent>(), "Welcher Tag ist heute", "Heute ist " + DateTime.Now.ToLongDateString()));
            intents.Add(new DialogIntent(speechService, null, loggerFactory.CreateLogger<DialogIntent>(), "Wo bist du", "Ich bin im " + speechService.Location, "Ich befinde mich auf einem Computer mit dem Namen " + Environment.MachineName, "Ich bin Prozess " + Process.GetCurrentProcess().Id));
            intents.Add(new DialogIntent(speechService, null, loggerFactory.CreateLogger<DialogIntent>(), "Wie geht es dir|Wie fühlst du dich", "Mir geht es ganz gut, danke!", "Ich fühl mich gut", "Alle meine Schaltkreise stehen unter Spannung"));
            intents.Add(new DialogIntent(speechService, null, loggerFactory.CreateLogger<DialogIntent>(), "Wie alt bist du|Wie lange[e]? lebst du [schon]?",
                () => { TimeSpan uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime); return "Ich bin bereits seit " + (int)uptime.TotalHours + " Stunden und " + uptime.Minutes + " Minuten am Leben."; }));
        }
    }
}
