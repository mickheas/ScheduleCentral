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
            
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var userName = _configuration["EmailSettings:Username"];
            // Fallback: if Username is not provided, use SenderEmail as the username
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = senderEmail;
            }

            var password = _configuration["EmailSettings:Password"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "CSMS";
            
            // Validate configuration — log warning but don't crash the app
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning(
                    "Email sender configuration is missing or incomplete (Host={Host}, User={User}, Sender={SenderEmail}). " +
                    "Email to {Email} with subject '{Subject}' was NOT sent. " +
                    "Please configure EmailSettings in appsettings.json or environment variables.",
                    host, userName, senderEmail, email, subject);
                
                // Fallback for debugging on Render without valid SMTP config: print the message contents to the logs
                _logger.LogInformation("--- EMAIL FALLBACK LOG ---");
                _logger.LogInformation("To: {Email}\nSubject: {Subject}\nBody: {Body}", email, subject, htmlMessage);
                _logger.LogInformation("--------------------------");
                return;
            }

            try
            {
                var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(userName, password),
                    EnableSsl = enableSsl,
                };

                // Use the SenderEmail for the "From" address, and optionally use the SenderName for the display name
                var fromAddress = new MailAddress(senderEmail, senderName);
                var toAddress = new MailAddress(email);

                var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}, subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}, subject: {Subject}", email, subject);
                
                // Print to logs as fallback if it fails
                _logger.LogInformation("--- EMAIL FALLBACK LOG (FAILED TO SEND) ---");
                _logger.LogInformation("To: {Email}\nSubject: {Subject}\nBody: {Body}", email, subject, htmlMessage);
                _logger.LogInformation("-------------------------------------------");
            }
        }
    }
}