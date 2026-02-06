using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using System.Diagnostics;


namespace ScheduleCentral.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifications;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            NotificationService notifications)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            };

            if (model.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.UserName = user.FirstName ?? user.UserName;
                    model.UserRoles = await _userManager.GetRolesAsync(user);

                    model.UnreadNotifications = await _notifications.GetUnreadCountAsync(user.Id);
                    model.RecentNotifications = await _notifications.GetRecentAsync(user.Id, take: 6);

                    // Instructor-specific stats for personalized dashboard
                    if (model.UserRoles.Any(r => string.Equals(r, "Instructor", StringComparison.OrdinalIgnoreCase)))
                    {
                        model.MyAvailableHours = user.AvailableHours;
                        model.MyCurrentLoad = user.CurrentLoad;
                        model.MyAssignedSections = user.AssignedSections?.Count ?? 0;

                        if (!string.IsNullOrWhiteSpace(user.AvailabilitySlots))
                        {
                            model.MyAvailabilitySlotsSelected = user.AvailabilitySlots
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Count(s => !string.IsNullOrWhiteSpace(s));
                        }
                    }
                }

                // FETCH REAL STATS (users)
                model.TotalUsers = await _userManager.Users.CountAsync();

                var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
                model.TotalInstructors = instructors.Count;

                var students = await _userManager.GetUsersInRoleAsync("Student");
                model.TotalStudents = students.Count;

                model.PendingApprovals = await _userManager.Users.CountAsync(u => !u.IsApproved);

                // SYSTEM / OFFERING STATS
                model.TotalDepartments = await _context.Departments.CountAsync();
                model.TotalCourses = await _context.Courses.CountAsync();
                model.TotalOfferings = await _context.CourseOfferings.CountAsync();
                model.ApprovedOfferings = await _context.CourseOfferings.CountAsync(o => o.Status == OfferingStatus.Approved);
                model.TotalSections = await _context.Sections.CountAsync();
                model.TotalScheduleGrids = await _context.ScheduleGrids.CountAsync();
                model.TotalMeetings = await _context.ScheduleMeetings.CountAsync();

                // Offerings grouped by status (for chart)
                var statusCounts = await _context.CourseOfferings
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                model.OfferingsByStatus = statusCounts
                    .ToDictionary(x => x.Status.ToString(), x => x.Count);

                // Determine primary role for dashboard styling: Department > TopManagement > ProgramOfficer > Instructor > Student
                var roles = model.UserRoles ?? new List<string>();
                string Pick(params string[] ordered)
                {
                    foreach (var r in ordered)
                    {
                        if (roles.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase)))
                            return r;
                    }
                    return roles.FirstOrDefault() ?? "";
                }

                model.PrimaryRole = Pick("Department", "TopManagement", "ProgramOfficer", "Instructor", "Student");
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
