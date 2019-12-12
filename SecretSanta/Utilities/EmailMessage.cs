using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;

namespace SecretSanta.Utilities
{
    public static class EmailMessage
    {
        public static void Send(IEnumerable<MailboxAddress> to, string subject, string body)
        {
            using (var smtp = new SmtpClient())
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress($"Secret Santa {DateHelper.Year}", AppSettings.SmtpFrom));
                message.To.AddRange(to);
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = body.Replace(Environment.NewLine, "<br />") };

                smtp.Connect(AppSettings.SmtpHost, AppSettings.SmtpPort, false);
                smtp.Authenticate(AppSettings.SmtpUser, AppSettings.SmtpPass);
                smtp.Send(message);
                smtp.Disconnect(true);
            }
        }
    }
}
