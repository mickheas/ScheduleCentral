using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using System.Security.Cryptography;

namespace ScheduleCentral.Services
{
    public class ScheduleSubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public ScheduleSubscriptionService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        private static string NewToken(int bytes = 32)
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
        }

        public async Task<(bool Success, bool AlreadyConfirmed, string? Error)> SubscribeAsync(
            string email,
            int sectionId,
            string academicYear,
            string semester,
            string confirmUrl,
            string unsubscribeUrl)
        {
            email = (email ?? "").Trim();
            academicYear = (academicYear ?? "").Trim();
            semester = (semester ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email))
                return (false, false, "Email is required.");
            if (sectionId <= 0)
                return (false, false, "Section is required.");
            if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
                return (false, false, "Academic year and semester are required.");

            var existing = await _context.ScheduleEmailSubscriptions
                .FirstOrDefaultAsync(s => s.Email == email
                                          && s.SectionId == sectionId
                                          && s.AcademicYear == academicYear
                                          && s.Semester == semester);

            if (existing != null && existing.ConfirmedAtUtc.HasValue)
                return (true, true, null);

            if (existing == null)
            {
                existing = new ScheduleEmailSubscription
                {
                    Email = email,
                    SectionId = sectionId,
                    AcademicYear = academicYear,
                    Semester = semester,
                    ConfirmToken = NewToken(),
                    UnsubscribeToken = NewToken(),
                    CreatedAtUtc = DateTime.UtcNow,
                    ConfirmedAtUtc = null
                };
                _context.ScheduleEmailSubscriptions.Add(existing);
            }
            else
            {
                existing.ConfirmToken = NewToken();
                if (string.IsNullOrWhiteSpace(existing.UnsubscribeToken))
                    existing.UnsubscribeToken = NewToken();
            }

            await _context.SaveChangesAsync();

            var confirmLink = (confirmUrl ?? "").Replace("__TOKEN__", existing.ConfirmToken);
            var unsubscribeLink = (unsubscribeUrl ?? "").Replace("__TOKEN__", existing.UnsubscribeToken);

            var subject = "Confirm your schedule subscription";
            var body = $"<p>Confirm your subscription for schedule updates.</p>" +
                       $"<p><a href=\"{confirmLink}\">Confirm subscription</a></p>" +
                       $"<p>If you did not request this, you can ignore this email.</p>" +
                       $"<p>Unsubscribe link (after confirming): <a href=\"{unsubscribeLink}\">Unsubscribe</a></p>";

            await _emailSender.SendEmailAsync(email, subject, body);
            return (true, false, null);
        }

        public async Task<bool> ConfirmAsync(string token)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return false;

            var sub = await _context.ScheduleEmailSubscriptions.FirstOrDefaultAsync(s => s.ConfirmToken == token);
            if (sub == null) return false;

            if (!sub.ConfirmedAtUtc.HasValue)
            {
                sub.ConfirmedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> UnsubscribeAsync(string token)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return false;

            var sub = await _context.ScheduleEmailSubscriptions.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
            if (sub == null) return false;

            _context.ScheduleEmailSubscriptions.Remove(sub);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SendUpdateAsync(int sectionId, string academicYear, string semester, string subject, string htmlMessage)
        {
            if (sectionId <= 0) return;

            var subs = await _context.ScheduleEmailSubscriptions
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId
                            && s.AcademicYear == academicYear
                            && s.Semester == semester
                            && s.ConfirmedAtUtc.HasValue)
                .Select(s => s.Email)
                .ToListAsync();

            foreach (var email in subs.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            }
        }
    }
}
