using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using Microsoft.AspNetCore.Identity;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
//using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// 1. Add the distributed memory cache (required for session state storage)
builder.Services.AddDistributedMemoryCache();

// 2. Configure and add the session service
builder.Services.AddSession(options =>
{
    // Configure session options here:
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session lasts 30 minutes of inactivity
    options.Cookie.HttpOnly = true;                 // Session cookie should not be accessible via client-side script
    options.Cookie.IsEssential = true;             // Make the session cookie essential
});
builder.Services.AddRazorPages();

builder.Services.AddTransient<IEmailSender, EmailSender>();

// Register the DataSeeder service
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<InstructorService>();
builder.Services.AddScoped<ScheduleSolverService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ScheduleSubscriptionService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// builder.Services.AddDataProtection()
//     .PersistKeysToDbContext<ApplicationDbContext>();
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() // Ensure Roles are enabled
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddTokenProvider<EmailTokenProvider<ApplicationUser>>("Email")
    .AddDefaultTokenProviders();

// Check if the application is running in Development mode
if (builder.Environment.IsDevelopment())
{
    // *** CRITICAL STEP: Override IEmailSender with the mock service ***
    // This ensures that ALL calls to IEmailSender (2FA, Forgot Password, Register)
    // safely log the email instead of attempting an SMTP connection.
    builder.Services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
}
else
{
    // In Production, you would configure a real email service here.
    // In Production, you would configure a real SMTP sender here, e.g.:
    // builder.Services.AddTransient<IEmailSender, RealSmtpEmailSender>();
    // For now, we leave it empty or use the default, which may fail
    // if not configured correctly.
}
var app = builder.Build();

// Run Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // This creates the tables if they don't exist
        await context.Database.MigrateAsync();

        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedDataAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();