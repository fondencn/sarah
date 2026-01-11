using Sarah.API.Interfaces;
using Services.Sarah.API.Interfaces.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class SceneIntent : Intent
    {
        public override string Title => "Szenen steuern";
        public override string HelpText => "Sag \"Aktiviere\" oder \"Deaktiviere\" und dann den Namen einer Scene um diese zu starten oder zu beenden.";
        private Regex ActivatePatternMatchExpression { get; }
        private Regex DeactivatePatternMatchExpression { get; }


        public SceneIntent(ISpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<SceneIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPatternActivate = "^.*(Aktiviere|Starte) (.+)$";
            string matchPatternDeactivate = "^.*(Deaktiviere|Beende) (.+)$";
            this.ActivatePatternMatchExpression = new Regex(matchPatternActivate, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            this.DeactivatePatternMatchExpression = new Regex(matchPatternDeactivate, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.ActivatePatternMatchExpression.IsMatch(recognizedSpeech) || this.DeactivatePatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            Match matchActivate = ActivatePatternMatchExpression.Match(recognizedSpeech);
            Match matchDeactivate = DeactivatePatternMatchExpression.Match(recognizedSpeech);

            try
            {
                if (matchActivate.Success)
                {
                    string sceneName = matchActivate.Groups[2].Value;
                    if (!String.IsNullOrWhiteSpace(sceneName))
                    {
                        _DeviceServiceClient.ActivateScene(sceneName);
                    } 
                } 
                else if (matchDeactivate.Success)
                {
                    string sceneName = matchActivate.Groups[2].Value;
                    if (!String.IsNullOrWhiteSpace(sceneName))
                    {
                        _DeviceServiceClient.DeactivateScene(sceneName);
                    }
                } 
                else
                {
                    throw new InvalidOperationException("Keines der Pattern hat getroffen -> Programmierfehler");
                }

                this.SaySuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SceneIntent");
                this.SayFail();
            }
        }
    }
}
