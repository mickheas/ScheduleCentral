using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ScheduleCentral.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Get settings from Configuration
            var host = _configuration["EmailSettings:SmtpHost"];
            var port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
            var userName = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];
            var senderName = _configuration["EmailSettings:SenderName"];
            
            // Validate configuration — log warning but don't crash the app
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning(
                    "Email sender configuration is missing or incomplete (Host={Host}, User={User}). " +
                    "Email to {Email} with subject '{Subject}' was NOT sent. " +
                    "Please configure EmailSettings in appsettings.json or environment variables.",
                    host, userName, email, subject);
                return;
            }

            try
            {
                var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(userName, password),
                    EnableSsl = enableSsl,
                };

                var mailMessage = new MailMessage(userName, email, subject, htmlMessage)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}, subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}, subject: {Subject}", email, subject);
            }
        }
    }
}