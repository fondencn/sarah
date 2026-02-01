using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class JokeIntent : Intent
    {

        public override string Title => "Witz erzählen";
        public override string HelpText => "Sag \"Erzähl einen Witz\", dann erzähle ich dir einen...";



        private Uri _witzeUri = new Uri("https://funny4you.at/webmasterprogramm/zufallswitz.php");
        private Regex PatternMatchExpression { get; }

        public JokeIntent(SpeechService speechService, IDeviceService deviceServiceClient, ILogger<JokeIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPattern = "^.*Erzähl(?:e)*\\s*(?:mir)*\\s*einen Witz.*$|^.*Wie wäre es mit einem Witz.*$|^.*Sag was lustiges.*$|^.*Sag(?:e)*\\s*(?:mir)*\\s*einen Witz.*$";
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
                HttpClient http = new HttpClient();
                string resultHtml = await http.GetStringAsync(_witzeUri);
                //Logger.Instance.LogDebug(resultHtml);
                string joke = resultHtml
                    .Replace("<br/>", " ")
                    .Replace("<br>", " ")
                    .Replace("<br />", " ");
                joke = System.Web.HttpUtility.HtmlDecode(joke);
                joke = joke.Substring(joke.IndexOf('>') + 1);
                joke = joke.Substring(0, joke.IndexOf('<'));

                this.Say(joke);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JokeIntent");
                this.SayFail();
            }
        }
    }
}
