using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using System.Text;
using System.Text.Encodings.Web;

namespace ScheduleCentral.Controllers
{
    // This controller handles the custom Instructor registration
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notifications;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            NotificationService notifications)
        {
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _notifications = notifications;
        }

        // GET: /Account/RegisterInstructor
        [HttpGet]
        public IActionResult RegisterInstructor()
        {
            return View();
        }

        // POST: /Account/RegisterInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterInstructor(InstructorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Email = (model.Email ?? "").Trim();

                var existing = await _userManager.FindByEmailAsync(model.Email);
                if (existing != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Already exists.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IsApproved = false, // <-- NEW INSTRUCTORS ARE NOT APPROVED
                    RegisteredAtUtc = DateTime.UtcNow,
                    IsSelfRegistered = true
                };

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("New instructor account created pending approval.");

                    // Add user to the "Instructor" role
                    await _userManager.AddToRoleAsync(user, "Instructor");

                    await _notifications.CreateForRolesAsync(
                        new[] { "ProgramOfficer", "Admin" },
                        "New instructor registration",
                        $"{user.FirstName} {user.LastName} ({user.Email}) registered and is pending approval.",
                        Url.Action("InstructorApprovals", "ProgramOfficer", null, Request.Scheme),
                        "InstructorApproval");

                    // Send set-password link (ResetPassword flow)
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = code, email = model.Email },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        model.Email,
                        "Set your password",
                        $"Your instructor account request has been received. You can set your password using this link: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Set Password</a>.\n\nNote: You will only be able to log in after an admin approves your account.");

                    // Redirect to a page informing them they need approval
                    return RedirectToAction("RegistrationPending");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Account/RegistrationPending
        [HttpGet]
        public IActionResult RegistrationPending()
        {
            return View();
        }
    }
}
