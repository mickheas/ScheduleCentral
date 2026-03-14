using ScheduleCentral.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ScheduleCentral.Services
{
    public class DataSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<DataSeeder> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedDataAsync()
        {
            // === 1. Ensure Roles Exist ===
            string[] roleNames = {
                "Admin",
                "ProgramOfficer",
                "Instructor",
                "Student",
                "Department",
                "TopManagement"
            };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create role: {RoleName}", roleName);
                        foreach (var error in result.Errors)
                        {
                            _logger.LogError("  Role Error: {ErrorDescription}", error.Description);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                }
            }

            // === 2. Ensure Default Admin User Exists ===
            // The migration seeds the admin with mikyasabebe76@gmail.com / Admin!23
            // This seeder acts as a fallback using config values
            var adminEmail = _configuration["DefaultAdmin:Email"] ?? "mikyasabebe76@gmail.com";
            var adminPassword = _configuration["DefaultAdmin:Password"] ?? "Admin!23";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                _logger.LogInformation("Attempting to create default Admin user: {AdminEmail}", adminEmail);

                var newAdminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    IsApproved = true
                };
                
                var result = await _userManager.CreateAsync(newAdminUser, adminPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin user created successfully. Assigning role...");
                    var roleResult = await _userManager.AddToRoleAsync(newAdminUser, "Admin");

                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to assign 'Admin' role to {AdminEmail}", adminEmail);
                        foreach (var error in roleResult.Errors)
                        {
                            _logger.LogError("  Role Error: {ErrorDescription}", error.Description);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Admin role assigned successfully.");
                    }
                    await _userManager.SetTwoFactorEnabledAsync(newAdminUser, false);
                }
                else
                {
                    _logger.LogError("Failed to create Admin user {AdminEmail}", adminEmail);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("  User Error: {ErrorDescription}", error.Description);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Admin user {AdminEmail} already exists. Skipping creation.", adminEmail);
                
                // Self-healing: If the old database wasn't dropped, the existing admin user might still have 2FA enabled
                // or have an unconfirmed email. We force-disable 2FA and force-confirm the email here.
                if (await _userManager.GetTwoFactorEnabledAsync(adminUser))
                {
                    _logger.LogWarning("Admin user {AdminEmail} had 2FA enabled. Force-disabling it...", adminEmail);
                    await _userManager.SetTwoFactorEnabledAsync(adminUser, false);
                }

                if (!await _userManager.IsEmailConfirmedAsync(adminUser))
                {
                    _logger.LogWarning("Admin user {AdminEmail} was not confirmed. Force-confirming...", adminEmail);
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(adminUser);
                    await _userManager.ConfirmEmailAsync(adminUser, token);
                }
            }
        }
    }
}