using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.Rules.Actions
{
    public class SendMailAction : RuleAction
    {

        public SendMailAction(string to, string subject, string body, IEmailNotifier emailNotifier)
        {
            this.EmailNotifier = emailNotifier;
            this.To = to;
            this.Subject = subject;
            this.Body = body;
        }

        private IEmailNotifier EmailNotifier { get; }
        private string To { get; set; }
        private string Subject { get; set; }
        private string Body { get; set; }

        public override void Execute(NetworkEvent sourceEvent)
        {
            try
            {
                this.EmailNotifier.Send(To, Subject, Body);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("Fehler beim behandeln einer Emailregel: " + ex.Message);
            }
        }
    }
}
