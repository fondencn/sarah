using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sarah.API.Interfaces.Service;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public static class IntentFactory
    {
        public static List<Intent> CreateIntents(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILEDService ledService)
        {
            if (speechService is null)
            {
                throw new ArgumentNullException(nameof(speechService));
            }

            List<Intent> intents = new List<Intent>();
            CreateChattyIntents(speechService, intents);
            intents.Add(new SwitchControlIntent(speechService, deviceServiceClient));
            intents.Add(new FlashlightIntent(speechService, deviceServiceClient, ledService));
            intents.Add(new HeatingControlIntent(speechService, deviceServiceClient));
            intents.Add(new CreateAlarmIntent(speechService, deviceServiceClient));
            intents.Add(new GetAlarmsIntent(speechService, deviceServiceClient));
            intents.Add(new JokeIntent(speechService, deviceServiceClient));
            intents.Add(new OpenDoorIntent(speechService, deviceServiceClient));
            intents.Add(new SceneIntent(speechService, deviceServiceClient));
            intents.Add(new ClockIntent(speechService, deviceServiceClient));
            intents.Add(new WeatherIntent(speechService, deviceServiceClient));
            intents.Add(new CoronaIntent(speechService, deviceServiceClient));
            intents.Add(new SilentIntent(speechService, deviceServiceClient));
            intents.Add(new FindPersonIntent(speechService, deviceServiceClient));
            intents.Add(new HelpIntent(speechService, deviceServiceClient));
            /* den NoMatchIntent als letztes, das ist der catch-all */
            intents.Add(new NoMatchIntent(speechService, deviceServiceClient));


            return intents;
        }

        /// <summary>
        /// Ein- und Ausgabe für  legere Unterhaltungen
        /// </summary>
        /// <param name="speechService"></param>
        /// <param name="intents"></param>
        private static void CreateChattyIntents(SpeechService speechService, List<Intent> intents)
        {
            intents.Add(new DialogIntent(speechService, null, "Wer bist du|[Wie|Was] ist dein Name|Wie heißt du", "Ich bin Sarah, eine künstliche Intelligenz.", "Mein Name ist Sarah", "Ich bin Sarah, ich kann dir helfen"));
            intents.Add(new DialogIntent(speechService, null, "Welcher Tag ist heute", "Heute ist " + DateTime.Now.ToLongDateString()));
            intents.Add(new DialogIntent(speechService, null, "Wo bist du", "Ich bin im " + speechService.Location, "Ich befinde mich auf einem Computer mit dem Namen " + Environment.MachineName, "Ich bin Prozess " + Process.GetCurrentProcess().Id));
            intents.Add(new DialogIntent(speechService, null, "Wie geht es dir|Wie fühlst du dich", "Mir geht es ganz gut, danke!", "Ich fühl mich gut", "Alle meine Schaltkreise stehen unter Spannung"));
            intents.Add(new DialogIntent(speechService, null, "Wie alt bist du|Wie lange[e]? lebst du [schon]?",
                () => { TimeSpan uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime); return "Ich bin bereits seit " + (int)uptime.TotalHours + " Stunden und " + uptime.Minutes + " Minuten am Leben."; }));
        }
    }
}
