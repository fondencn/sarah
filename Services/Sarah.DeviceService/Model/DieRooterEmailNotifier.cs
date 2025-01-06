using Sarah.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Sarah.DeviceService.Model
{
    internal class DieRooterEmailNotifier : IEmailNotifier
    {
        public DieRooterEmailNotifier()
        {
            

        }

        public void Send(string receipient, string subject, string body)
        {
            using (System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("w0073110.kasserver.com", 25))
            {
                client.EnableSsl = true;
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                client.Credentials = new NetworkCredential("m00eb67d", "normales");
                using (System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage("c.fonden@die-rooter.de", receipient))
                {
                    msg.Subject = subject;
                    msg.Body = body;
                    client.Send(msg);
                }
            }
        }
    }
}
