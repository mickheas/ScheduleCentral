using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ScheduleCentral.Services
{
    // Make sure you have the necessary SMTP settings in appsettings.json
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        // IConfiguration is injected to read SMTP settings from appsettings.json
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Get settings from Configuration
            var host = _configuration["EmailSettings:SmtpHost"];
            var port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
            var userName = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];
            var senderName = _configuration["EmailSettings:SenderName"];
            
            // Basic validation
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                // In a real app, log this error instead of throwing
                throw new InvalidOperationException("Email sender configuration is missing or incomplete.");
            }

            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = enableSsl,
            };

            var mailMessage = new MailMessage(userName, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            return client.SendMailAsync(mailMessage);
        }
    }
}