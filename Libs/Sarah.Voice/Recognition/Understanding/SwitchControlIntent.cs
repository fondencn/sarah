using Sarah.API.Interfaces;
using Services.Sarah.API.Interfaces.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class SwitchControlIntent : Intent
    {
        public override string Title => "Licht oder Steckdosen steuern";

        public override string HelpText => "Sag \"Licht im Wohnzimmer einschalten\" oder \"Treppenlicht einschalten\" oder \"Licht aus\" oder einfach nur \"Licht\" ohne Raumangabe für diesen Raum. ";

        private Regex PatternMatchExpression { get; } //Lampen in Räumen an oder aus
        private Regex PatternMatchExpressionToggle { get; } //Lampen in Räumen umschalten
        private Regex PatternMatchExpressionKaffee { get; } //Kaffee kochen

        public SwitchControlIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<SwitchControlIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPattern = @"^(Licht)?\s*(\w*)\s*(im\s*(\w*)\s*)?(an|ein|aus)[schalten]*$";
            string matchPattern2 = @"^\w*Licht\w*$";
            string matchPattern3 = @"^Koch Kaffee$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            this.PatternMatchExpressionToggle = new Regex(matchPattern2, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            this.PatternMatchExpressionKaffee = new Regex(matchPattern3, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech)
                || this.PatternMatchExpressionToggle.IsMatch(recognizedSpeech)
                || this.PatternMatchExpressionKaffee.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                Match m = this.PatternMatchExpression.Match(recognizedSpeech);
                if (m.Success)
                {
                    string lampname = m.Groups[2].Value;
                    string roomname = m.Groups[4].Value;
                    string onoff = m.Groups[5].Value;
                    if (String.IsNullOrWhiteSpace(roomname) && String.IsNullOrWhiteSpace(lampname))
                    {
                        /* dann sind alle Geräte im aktuellen Raum gemeint */
                        roomname = this.Location;
                    }
                    bool on = onoff.Equals("an", StringComparison.CurrentCultureIgnoreCase)
                        || onoff.Equals("ein", StringComparison.CurrentCultureIgnoreCase);

                    await _DeviceServiceClient.SetLampByRoom(roomname, lampname, on);
                } 
                else
                {
                    m = this.PatternMatchExpressionKaffee.Match(recognizedSpeech);
                    if (m.Success)
                    {
                        string lampname = "Kaffeemaschine";
                        bool on = true;
                        await _DeviceServiceClient.SetLampByRoom(null, lampname, on);
                    } else
                    {
                        m = this.PatternMatchExpressionToggle.Match(recognizedSpeech);
                        if (m.Success)
                        {
                            await _DeviceServiceClient.ToggleLampByRoom(this.Location, null);
                        }
                    }
                }

                this.SaySuccess();
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SwitchControlIntent");
                this.SayFail();
            }
        }
    }
}
