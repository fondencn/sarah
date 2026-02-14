using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    /// <summary>
    /// Hilfesystem für die Sprachunterstützung. 
    /// </summary>
    public class HelpIntent : Intent
    {

        public override string Title => "Hilfe";
        public override string HelpText => "Sag \"Hilfe\" für eine Übersicht oder \"Hilfe zu Licht Steuern\" für Hilfe zu einem bestimmten Thema";

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="speechService"></param>
        public HelpIntent(ISpeechService speechService, IDeviceService deviceServiceClient, ILogger<HelpIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
        }

        /// <summary>
        /// Pattern
        /// </summary>
        private Regex PatternMatchExpression { get; } = new Regex("^Hilfe\\s*(zu |für )*(.*)$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <inheritdoc/>
        protected override bool CanHandleInternal(string recognizedSpeech) => PatternMatchExpression.IsMatch(recognizedSpeech);

        /// <inheritdoc/>
        protected override void HandleInternal(string recognizedSpeech)
        {
            Match m = PatternMatchExpression.Match(recognizedSpeech);
            string intentName = m.Groups[2].Value;
            ShowIntentHelp(intentName);
        }

        /// <summary>
        /// Zeigt die Hilfe zum übergebenen Intent an
        /// oder eine allgemeine Hilfe, falls der Intentname leer ist. 
        /// </summary>
        /// <param name="intentName">Name eines Intents oder leer für eine allgemeine Hilfe</param>
        private void ShowIntentHelp(string intentName)
        {
            string helpMessage = null;
            if(String.IsNullOrWhiteSpace(intentName))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Ich kann folgendes für dich tun: ");
                sb.Append(String.Join(", ", 
                    Intents.Instance.KnownIntents
                        .Where(i => !String.IsNullOrWhiteSpace(i.Title))
                        .Select(i => i.Title)
                        .Distinct()));
                sb.Append(". Für Hilfe zu einem bestimmten Thema, sage \"Hilfe zu\" und dann das Thema.");
                helpMessage = sb.ToString();
            } 
            else
            {
                Intent i = Intents.Instance.KnownIntents.FirstOrDefault(item => String.Equals(intentName, item.Title, StringComparison.OrdinalIgnoreCase));
                if (i != null)
                {
                    helpMessage = i.HelpText;
                }
            }

            if(String.IsNullOrWhiteSpace(helpMessage))
            {
                helpMessage = "Dafür ist leider keine Hilfe verfügbar. ";
            }

            this.Say(helpMessage);
        }
    }
}
