using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Ensure this is present

namespace ScheduleCentral.Services
{
    // This service implements the IEmailSender interface
    // but only logs the email to the console/debug output
    // to prevent SMTP connection errors in development.
    public class DevelopmentEmailSender : IEmailSender
    {
        private readonly ILogger<DevelopmentEmailSender> _logger;

        public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log the email attempt instead of trying to send it.
            _logger.LogInformation("--- DEVELOPMENT EMAIL SIMULATION ---");
            _logger.LogInformation("EMAIL BYPASSED: To: {Email}", email);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Body:\n{HtmlMessage}", htmlMessage);
            _logger.LogInformation("------------------------------------");

            // Return a completed task to simulate successful sending
            return Task.CompletedTask;
        }
    }
}