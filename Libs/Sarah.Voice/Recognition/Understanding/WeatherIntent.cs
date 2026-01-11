using Sarah.API.Interfaces;
using Microsoft.Recognizers.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Services.Sarah.API.Interfaces.Service;
using Sarah.API.BusinessObjects.SpeakerRequests;

namespace Sarah.Voice.Recognition.Understanding
{
    public class WeatherIntent : Intent
    {

        public override string Title => "Wettervorhersage und Wetterwarnungen";
        public override string HelpText => "Sag \"Wie ist das Wetter?\" für die Wettervorhersage";



        private Regex PatternMatchExpression { get; }

        public WeatherIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<WeatherIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPattern = "^.*Wetter\\s*(.*)$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                Match m = this.PatternMatchExpression.Match(recognizedSpeech);
                string timeString = m.Groups[1].Value;
                if (!String.IsNullOrEmpty(timeString))
                {
                    var recognizedDates = Microsoft.Recognizers.Text.DateTime.DateTimeRecognizer.RecognizeDateTime(timeString, Culture.German);
                    List<Dictionary<string, string>> first = (List<Dictionary<string, string>>)recognizedDates.First().Resolution.Values.First();
                    string sdt = first[0]["value"];
                    DateTime targetDate = DateTime.Parse(sdt);

                    GetWeatherResponse response = await _DeviceServiceClient.GetWeatherForecastInfo(targetDate);
                    this.Say(String.IsNullOrWhiteSpace(response.WeatherInfo) ? "Es liegen keine Wetterdaten für " + timeString + " vor." : response.WeatherInfo);
                }
                else
                {

                    GetWeatherResponse response = await _DeviceServiceClient.GetWeatherInfo();
                    this.Say(String.IsNullOrWhiteSpace(response.WeatherInfo) ? "Es liegen keine Wetterdaten vor." : response.WeatherInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WeatherIntent");
                this.SayFail();
            }
        }
    }
}
