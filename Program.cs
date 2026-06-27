using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using Microsoft.AspNetCore.Identity;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

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
builder.Services.AddHttpContextAccessor();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string? demoRole = null;
        try
        {
            demoRole = httpContext.Session?.GetString("DemoRole");
        }
        catch (Exception)
        {
        }

        if (!string.IsNullOrEmpty(demoRole))
        {
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Session?.Id ?? "default",
                factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 30,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                });
        }
        return System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("NoLimit");
    });
});

// Register the DataSeeder and other services
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<InstructorService>();
builder.Services.AddScoped<ScheduleSolverService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ScheduleSubscriptionService>();

builder.Services.AddScoped<DbContextOptions<ApplicationDbContext>>(serviceProvider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;

    string? demoRole = null;
    try
    {
        demoRole = httpContext?.Session?.GetString("DemoRole");
    }
    catch (Exception)
    {
    }

    if (!string.IsNullOrEmpty(demoRole))
    {
        var sessionId = httpContext!.Session.Id;
        var conn = ScheduleCentral.Services.DemoDbContextConnectionCache.GetConnection(sessionId);
        optionsBuilder.UseSqlite(conn);
    }
    else
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var connStr = config.GetConnectionString("DefaultConnection");
        optionsBuilder.UseNpgsql(connStr, npgsqlOptions =>
        {
            // Retry on transient failures (e.g. Render free-tier Postgres going idle)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(60);
        });
    }

    return optionsBuilder.Options;
});

builder.Services.AddScoped<ApplicationDbContext>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // Ensure Roles are enabled
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddTokenProvider<EmailTokenProvider<ApplicationUser>>("Email")
    .AddDefaultTokenProviders();

// Register the email sender based on environment
if (builder.Environment.IsDevelopment())
{
    // In Development: log emails instead of sending them
    builder.Services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
}
else
{
    // In Production: use the real SMTP email sender
    builder.Services.AddTransient<IEmailSender, EmailSender>();
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

app.UseSession();
app.UseRateLimiter();
app.UseMiddleware<ScheduleCentral.Middleware.DemoAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();