using Sarah.API.BusinessObjects.SpeakerRequests;
using Sarah.API.Interfaces;
using Sarah.Logging;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class GetAlarmsIntent : Intent
    {
        public override string HelpText => "Sag \"Wie sind meine Termine heute\" oder \"Erinnerungen für heute\" ";
        public override string Title => "Erinnerungen abfragen";

        private Regex PatternMatchExpression { get; }


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="speechService"></param>
        public GetAlarmsIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
            string matchPattern = @"^.*(Termine|Erinnerungen).*(heute).*$";
            this.PatternMatchExpression= new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }


        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected async override void HandleInternal(string recognizedSpeech)
        {
            try
            {
                GetAlarmSchedulesResponse response = await _DeviceServiceClient.GetAlarmSchedules();
                if (String.IsNullOrWhiteSpace(response?.AlarmScheduleInfos))
                {
                    this.Say("Mir sind keine Termine für heute bekannt.");
                }
                else
                {
                    this.Say("Für heute sind folgende Termine vorgemerkt: \r\n" + response.AlarmScheduleInfos);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(nameof(GetAlarmsIntent), ex);
                this.SayFail();
            }
        }
    }
}
