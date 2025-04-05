using Sarah.Logging;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class HeatingControlIntent : Intent
    {

        public override string Title => "Heizung steuern";
        public override string HelpText => "Sag \"Heizung im Wohnzimmer einschalten\" oder \"Heizung aus\" ohne Raumangabe für diesen Raum. ";
        private Regex PatternMatchExpression { get; }

        public HeatingControlIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
            string matchPattern = @"^.*Heizung\s*(im\s*(\w*)\s*)?(an|ein|aus)[schalten]*$";
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
                string roomname = m.Groups[2].Value;
                if (String.IsNullOrWhiteSpace(roomname))
                {
                    roomname = this.Location;
                }
                string onoff = m.Groups[3].Value;
                bool on = onoff.Equals("an", StringComparison.CurrentCultureIgnoreCase)
                    || onoff.Equals("ein", StringComparison.CurrentCultureIgnoreCase);

                await _DeviceServiceClient.SetTemperatureByRoom(roomname, on ? 28 : 8);

                this.SaySuccess();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("HeatingControlIntent", ex);
                this.SayFail();
            }
        }
    }
}
