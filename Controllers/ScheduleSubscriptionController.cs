using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using System;
using System.Linq;

namespace ScheduleCentral.Controllers
{
    [AllowAnonymous]
    public class ScheduleSubscriptionController : Controller
    {
        private readonly ScheduleSubscriptionService _subs;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ScheduleSubscriptionController> _logger;

        public ScheduleSubscriptionController(
            ScheduleSubscriptionService subs,
            UserManager<ApplicationUser> userManager,
            ILogger<ScheduleSubscriptionController> logger)
        {
            _subs = subs;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(string email, int sectionId, string academicYear, string semester, string? returnUrl = null)
        {
            try
            {
                var confirmUrlTemplate = Url.Action("Confirm", "ScheduleSubscription", new { token = "__TOKEN__", returnUrl }, Request.Scheme) ?? "";
                var unsubscribeUrlTemplate = Url.Action("Unsubscribe", "ScheduleSubscription", new { token = "__TOKEN__", returnUrl }, Request.Scheme) ?? "";

                var result = await _subs.SubscribeAsync(email, sectionId, academicYear, semester,
                    confirmUrlTemplate,
                    unsubscribeUrlTemplate);

                if (!result.Success)
                {
                    TempData["Error"] = result.Error ?? "Failed to subscribe.";
                }
                else if (result.AlreadyConfirmed)
                {
                    TempData["Success"] = "You are already subscribed for this section.";

                    await EnsureStudentUserAsync(email);
                }
                else
                {
                    TempData["Success"] = "Subscription created. Please check your email to confirm.";

                    await EnsureStudentUserAsync(email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe");
                TempData["Error"] = "Failed to subscribe.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Public", "Schedule", new { academicYear, semester, sectionId });
        }

        private async Task EnsureStudentUserAsync(string email)
        {
            email = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email)) return;

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = email,
                        LastName = "",
                        IsApproved = true,
                        RegisteredAtUtc = DateTime.UtcNow,
                        IsSelfRegistered = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to create Student user for subscription email {Email}: {Errors}",
                            email,
                            string.Join("; ", createResult.Errors.Select(e => e.Description)));
                        return;
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, "Student"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to add subscription user {Email} to Student role: {Errors}",
                            email,
                            string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure Student user for subscription email {Email}", email);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(string token, string? returnUrl = null)
        {
            var ok = await _subs.ConfirmAsync(token);
            TempData[ok ? "Success" : "Error"] = ok ? "Subscription confirmed." : "Invalid confirmation link.";

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Public", "Schedule");
        }

        [HttpGet]
        public async Task<IActionResult> Unsubscribe(string token, string? returnUrl = null)
        {
            var ok = await _subs.UnsubscribeAsync(token);
            TempData[ok ? "Success" : "Error"] = ok ? "You have been unsubscribed." : "Invalid unsubscribe link.";

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Public", "Schedule");
        }
    }
}
