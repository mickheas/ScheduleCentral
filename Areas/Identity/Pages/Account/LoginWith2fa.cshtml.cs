using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // For IEmailSender
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ScheduleCentral.Models; // Your custom ApplicationUser model

namespace ScheduleCentral.Areas.Identity.Pages.Account
{
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginWith2faModel> _logger;
        private readonly IEmailSender _emailSender; // 👈 INJECTED

        public LoginWith2faModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginWith2faModel> logger,
            IEmailSender emailSender) // 👈 ADDED TO CONSTRUCTOR
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender; // 👈 ASSIGNED
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string StatusMessage { get; set; } // Added for user feedback

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Two-Factor Code")]
            public string TwoFactorCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            // --- CUSTOM LOGIC TO SEND EMAIL CODE ---
            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);

            // Check if Email provider is available
            if (providers.Contains("Email"))
            {
                _logger.LogInformation("2FA via Email requested. Generating token.");
                
                // 1. Generate the token
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                
                // 2. Send the email using the registered service
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Your 2FA Security Code",
                    $"Your two-factor authentication code is: <b>{token}</b>. This code is valid for a short time."
                );
                StatusMessage = $"A two-factor code has been sent to your email address: {user.Email}.";
            }
            else
            {
                // Fallback or default message if Email isn't an option
                StatusMessage = "Please use your Authenticator App to get the code.";
            }
            // ----------------------------------------

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            
            // This method checks *all* valid providers (Authenticator, Email, Phone) automatically
            var result = await _signInManager.TwoFactorSignInAsync("Email", authenticatorCode, rememberMe, Input.TwoFactorCode != authenticatorCode);
            
            // If the above line fails, it attempts to check the Authenticator app as well.
            if (!result.Succeeded && Input.TwoFactorCode != authenticatorCode)
            {
                result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.TwoFactorCode != authenticatorCode);
            }


            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", await _userManager.GetUserIdAsync(user));
                return LocalRedirect(Url.Content("~/"));
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID '{UserId}' account locked out.", await _userManager.GetUserIdAsync(user));
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", await _userManager.GetUserIdAsync(user));
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
                return Page();
            }
        }
    }
}