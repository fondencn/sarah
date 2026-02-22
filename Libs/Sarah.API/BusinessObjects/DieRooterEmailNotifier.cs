using Microsoft.Extensions.Configuration;
using Sarah.API.Interfaces;
using System;
using System.Net;
using System.Security;

namespace Sarah.API.Businessobjects
{
    public class DieRooterEmailNotifier : IEmailNotifier
    {
        private string? _smtpServer;
        private int _smtpPort;
        private string? _smtpUser;
        private SecureString? _smtpPassword;
        private string? _smptSender;

        public DieRooterEmailNotifier(IConfiguration config)
        {
            this._smptSender = config["EmailNotifier:SmtpSender"] ?? throw new ArgumentNullException("EmailNotifier:SmtpSender configuration is missing");
            this._smtpServer = config["EmailNotifier:SmtpServer"] ?? throw new ArgumentNullException("EmailNotifier:SmtpServer configuration is missing");
            this._smtpPort = int.Parse(config["EmailNotifier:SmtpPort"] ?? throw new ArgumentNullException("EmailNotifier:SmtpPort configuration is missing"));
            this._smtpUser = config["EmailNotifier:SmtpUsername"] ?? throw new ArgumentNullException("EmailNotifier:SmtpUsername configuration is missing");
            this._smtpPassword = new SecureString();
            foreach (char c in config["EmailNotifier:SmtpPassword"] ?? throw new ArgumentNullException("EmailNotifier:SmtpPassword configuration is missing"))
            {
                this._smtpPassword.AppendChar(c);
            }
        }

        public void Send(string receipient, string subject, string body)
        {
            using (System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                using (System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(_smptSender, receipient))
                {
                    msg.Subject = subject;
                    msg.Body = body;
                    client.Send(msg);
                }
            }
        }
    }
}
