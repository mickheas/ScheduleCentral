using ScheduleCentral.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics; // Added for Debug.WriteLine

namespace ScheduleCentral.Services
{
    public class DataSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DataSeeder(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task SeedDataAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // === 1. Create Roles ===
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
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                        if (!result.Succeeded)
                        {
                            Debug.WriteLine($"Failed to create role: {roleName}");
                            foreach (var error in result.Errors)
                            {
                                Debug.WriteLine($"\tError: {error.Description}");
                            }
                        }
                    }
                }


                 // === 2. Create Default Admin User ===
                // Use IConfiguration to retrieve credentials, defaulting to secure local values
                var adminEmail = _configuration["DefaultAdmin:Email"] ?? "mikyasabebe76@gmail.com";
                var adminPassword = _configuration["DefaultAdmin:Password"] ?? "Admin!23";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    Debug.WriteLine($"Attempting to create default Admin user: {adminEmail}");

                    var newAdminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Mikyas",
                        LastName = "Abebe",
                        EmailConfirmed = true,
                        IsApproved = true // Admin is approved by default
                    };
                    
                    var result = await userManager.CreateAsync(newAdminUser, adminPassword);

                    if (result.Succeeded)
                    {
                        Debug.WriteLine("Admin user created successfully. Assigning role...");
                        // Assign the Admin role
                        var roleResult = await userManager.AddToRoleAsync(newAdminUser, "Admin");

                        if (!roleResult.Succeeded)
                        {
                            Debug.WriteLine($"Failed to assign 'Admin' role to {adminEmail}.");
                            foreach (var error in roleResult.Errors)
                            {
                                Debug.WriteLine($"\tRole Error: {error.Description}");
                            }
                        } else {
                            Debug.WriteLine("Admin role assigned successfully.");
                        }
                        await userManager.SetTwoFactorEnabledAsync(newAdminUser, true);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to create Admin user {adminEmail}.");
                        foreach (var error in result.Errors)
                        {
                            Debug.WriteLine($"\tUser Error: {error.Description}");
                        }
                    }
                } else {
                    Debug.WriteLine($"Admin user {adminEmail} already exists. Skipping creation.");
                }
                

            }
        }
    }
}