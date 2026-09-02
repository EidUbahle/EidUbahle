using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace WamoApp
{
    public static class EmailService
    {
        public static void Send(string to, string subject, string body)
        {
            var message = new MailMessage { From = new MailAddress(ConfigurationManager.AppSettings["SmtpFromEmail"], ConfigurationManager.AppSettings["SmtpFromName"]), Subject = subject, Body = body, IsBodyHtml = true };
            message.To.Add(to);
            using (var client = new SmtpClient(ConfigurationManager.AppSettings["SmtpHost"], int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "25")))
            {
                client.EnableSsl = string.Equals(ConfigurationManager.AppSettings["EnableSsl"], "true", StringComparison.OrdinalIgnoreCase);
                client.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["SmtpUsername"], ConfigurationManager.AppSettings["SmtpPassword"]);
                client.Send(message);
            }
        }
    }
}
