using Sarah.API.Interfaces;
using Sarah.Logging;
using Microsoft.Recognizers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public class SilentIntent : Intent
    {

        public override string Title => "Lautsprecher für eine Zeit still schalten";
        public override string HelpText => "Sag \"Sei eine Stunde still\" um mich stumm zu schalten";



        private Regex PatternMatchExpression { get; }

        public SilentIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
            string matchPattern = "^Sei[t]* (.+) still$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            try
            {
                Match m = this.PatternMatchExpression.Match(recognizedSpeech);
                string timeString = m.Groups[1].Value;

                if (timeString.Contains("nicht mehr", StringComparison.OrdinalIgnoreCase))
                {
                    this.SpeechService.DeactivateSilentTime();
                    this.SaySuccess();
                }
                else
                {
                    var recognizedDates = Microsoft.Recognizers.Text.DateTime.DateTimeRecognizer.RecognizeDateTime(timeString, Culture.German);
                    List<Dictionary<string, string>> first = (List<Dictionary<string, string>>)recognizedDates.First().Resolution.Values.First();
                    int recognizedSeconds = int.Parse(first[0]["value"]);
                    if (recognizedSeconds <= 0)
                    {
                        this.SpeechService.DeactivateSilentTime();
                        this.SaySuccess();
                    }
                    else
                    {
                        DateTime silenceEnd = DateTime.Now.AddSeconds(recognizedSeconds);
                        if (silenceEnd > DateTime.Now)
                        {
                            this.Say("OK, ich bin still bis " + silenceEnd.ToString("HH:mm"));
                            this.SpeechService.ActivateSilentTime(silenceEnd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("SilentIntent", ex);
                this.SayFail();
            }
        }
    }
}
