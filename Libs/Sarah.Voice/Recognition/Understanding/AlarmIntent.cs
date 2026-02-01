using Sarah.API.Interfaces;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sarah.API.Interfaces.Services;

namespace Sarah.Voice.Recognition.Understanding
{
    public class CreateAlarmIntent : Intent
    {
        public override string Title => "Erinnerung erstellen";
        public override string HelpText => "Sag \"Erinnere mich in 5 Minuten an...\" oder \"Erinnere mich Morgen um 9:30 an... \" ";


        private Regex PatternMatchExpressionTimeSpan { get; }
        private Regex PatternMatchExpressionTime { get; }

        public CreateAlarmIntent(ISpeechService speechService, IDeviceService deviceServiceClient, ILogger<CreateAlarmIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPatternTimeSpan = @"^.*Erinnere mich in (.+)\s*(Minuten|Minute|Stunden|Stunde|Tagen|Tag)\s*((?:an)*\s*(.)*)$";
            this.PatternMatchExpressionTimeSpan = new Regex(matchPatternTimeSpan, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string matchPatternTime = @"^.*Erinnere mich (.+)\s*(an\s*(.)*)$";
            this.PatternMatchExpressionTime = new Regex(matchPatternTime, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpressionTimeSpan.IsMatch(recognizedSpeech) || this.PatternMatchExpressionTime.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                DateTime reminderTime;
                string reminderText;
                if (this.PatternMatchExpressionTimeSpan.IsMatch(recognizedSpeech))
                {
                    Match m = this.PatternMatchExpressionTimeSpan.Match(recognizedSpeech);
                    string timeValue = m.Groups[1].Value;
                    string timeUnit = m.Groups[2].Value;
                    string text = m.Groups[3].Value;

                    var numbers = NumberRecognizer.RecognizeNumber(timeValue, Culture.German, NumberOptions.None, false);
                    string sVal = numbers.First().Resolution["value"] as string;
                    double dVal = double.Parse(sVal);

                    reminderTime = DateTime.Now.Add(dVal, timeUnit);
                    reminderText = text;
                }
                else
                {
                    Match m = this.PatternMatchExpressionTime.Match(recognizedSpeech);
                    string timeString = m.Groups[1].Value;
                    string text = m.Groups[2].Value;
                    var recognizedDates = Microsoft.Recognizers.Text.DateTime.DateTimeRecognizer.RecognizeDateTime(timeString, Culture.German);
                    List<Dictionary<string,string>> first = (List < Dictionary<string, string> > )recognizedDates.First().Resolution.Values.First();
                    string sdt = first[0]["value"];
                    reminderTime = DateTime.Parse(sdt);

                    reminderText = text;
                }
                await _DeviceServiceClient.SetAlarmSchedule(reminderText, reminderTime, "BroadcastAllSpeakers");

                this.SaySuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {IntentName}", nameof(CreateAlarmIntent));
                this.SayFail();
            }
        }

        private DateTime ParseDateTime(string hours, string minutes)
        {
            DateTime dt = DateTime.Today.AddHours(int.Parse(hours)).AddMinutes(int.Parse(minutes));
            if(dt < DateTime.Now)
            {
                dt = dt.AddDays(1);
            }
            return dt;
        }

    }

    public static class DateTimeExtensions
    {

        public static DateTime Add(this DateTime dt, double timeValue, string timeUnit)
        {
            switch (timeUnit.Trim())
            {
                case "Stunde":
                case "Stunden":
                    return dt.AddHours(timeValue);

                case "Minute":
                case "Minuten":
                    return dt.AddMinutes(timeValue);

                case "Sekunde":
                case "Sekunden":
                    return dt.AddSeconds(timeValue);

            }
            return dt;
        }
    }
}
