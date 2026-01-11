using Sarah.API.BusinessObjects.SpeakerRequests;
using Sarah.API.Interfaces;
using Services.Sarah.API.Interfaces.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class FindPersonIntent : Intent
    {
        public override string Title => "Personen lokalisieren erstellen";
        public override string HelpText => "Sag \"Finde Captain Picard.\" oder \"Wo ist Captain Picard? \" ";


        private Regex PatternMatchExpression1 { get; }
        private Regex PatternMatchExpression2 { get; }
        private Regex PatternMatchExpression3 { get; }

        public FindPersonIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<FindPersonIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPattern1 = @"^Wo ist (.+).*$";
            string matchPattern2 = @"^Finde (.+).*$";
            string matchPattern3 = @"^Lokalisiere (.+).*$";
            this.PatternMatchExpression1 = new Regex(matchPattern1, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            this.PatternMatchExpression2 = new Regex(matchPattern2, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            this.PatternMatchExpression3 = new Regex(matchPattern3, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression1.IsMatch(recognizedSpeech)
                || this.PatternMatchExpression2.IsMatch(recognizedSpeech)
                || this.PatternMatchExpression3.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                string person;

                Match m1;
                Match m2;
                Match m3;
                m1 = PatternMatchExpression1.Match(recognizedSpeech);
                m2 = PatternMatchExpression2.Match(recognizedSpeech);
                m3 = PatternMatchExpression3.Match(recognizedSpeech);
                if (m1.Success)
                {
                    person = m1.Groups[1].Value;
                }
                else if (m2.Success)
                {
                    person = m2.Groups[1].Value;
                }
                else if (m3.Success)
                {
                    person = m3.Groups[1].Value;
                }
                else
                {
                    person = null;
                }

                if (!String.IsNullOrEmpty(person))
                {
                    GetPersonLocationResponse result = await _DeviceServiceClient.GetPersonLocation(person);
                    this.Say(result.Message);
                } 
                else
                {
                    this.Say("Ich verstehe nicht, welche Person du suchst.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {IntentName}", nameof(FindPersonIntent));
                this.SayFail();
            }
        }

        private DateTime ParseDateTime(string hours, string minutes)
        {
            DateTime dt = DateTime.Today.AddHours(int.Parse(hours)).AddMinutes(int.Parse(minutes));
            if (dt < DateTime.Now)
            {
                dt = dt.AddDays(1);
            }
            return dt;
        }

    }

}
