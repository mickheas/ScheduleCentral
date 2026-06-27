using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ScheduleCentral.Middleware
{
    public class DemoAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public DemoAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var session = context.Session;
            string? demoRole = null;
            try
            {
                demoRole = session?.GetString("DemoRole");
            }
            catch (Exception)
            {
                // Session is not available/loaded yet in this request stage
            }

            if (!string.IsNullOrEmpty(demoRole))
            {
                context.Items["IsDemo"] = true;

                // 1. Ensure the in-memory SQLite database is initialized and seeded for this session
                if (session!.GetInt32("DemoDbSeeded") == null)
                {
                    using (var scope = context.RequestServices.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        // Triggers table creation from the EF core model schema in memory
                        await dbContext.Database.EnsureCreatedAsync();

                        // Populate SQLite tables with seed data
                        await DemoDataSeeder.SeedAsync(dbContext, scope.ServiceProvider);
                    }
                    session.SetInt32("DemoDbSeeded", 1);
                }

                // 2. Inject mock ClaimsPrincipal matching the requested demo role
                var (userName, userId, firstName, lastName) = demoRole switch
                {
                    "Admin" => ("admin.demo@demo.edu", "demo_admin_id", "Demo", "Admin"),
                    "ProgramOfficer" => ("po.demo@demo.edu", "demo_po_id", "Demo", "Program Officer"),
                    "Department" => ("dept.demo@demo.edu", "demo_dept_id", "Demo", "Department Head"),
                    "Instructor" => ("instructor.demo@demo.edu", "demo_instructor_id", "Alice", "Smith"),
                    "TopManagement" => ("exec.demo@demo.edu", "demo_exec_id", "Demo", "Top Management"),
                    _ => ("student.demo@demo.edu", "demo_student_id", "Demo", "Student")
                };

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, userName),
                    new Claim(ClaimTypes.Role, demoRole),
                    new Claim("DemoMode", "true")
                };

                var identity = new ClaimsIdentity(claims, "DemoAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            await _next(context);
        }
    }
}
