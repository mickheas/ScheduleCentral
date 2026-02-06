// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using ScheduleCentral.Data;
// using ScheduleCentral.Models;

// namespace ScheduleCentral.Controllers
// {
//     [Authorize(Roles = "ProgramOfficer,Admin")]
//     public class ProgramOfficerController : Controller
//     {
//         private readonly ApplicationDbContext _context;

//         public ProgramOfficerController(ApplicationDbContext context)
//         {
//             _context = context;
//         }

//         // === MANAGE ROOMS ===
//         public async Task<IActionResult> Rooms()
//         {
//             return View(await _context.Rooms.ToListAsync());
//         }

//         [HttpPost]
//         public async Task<IActionResult> CreateRoom(Room room)
//         {
//             if (ModelState.IsValid)
//             {
//                 _context.Rooms.Add(room);
//                 await _context.SaveChangesAsync();
//                 TempData["Success"] = "Room created successfully.";
//             }
//             return RedirectToAction(nameof(Rooms));
//         }

//         // === MANAGE COURSES ===
//         public async Task<IActionResult> Courses()
//         {
//             return View(await _context.Courses.ToListAsync());
//         }

//         [HttpPost]
//         public async Task<IActionResult> CreateCourse(Course course)
//         {
//             if (ModelState.IsValid)
//             {
//                 _context.Courses.Add(course);
//                 await _context.SaveChangesAsync();
//                 TempData["Success"] = "Course created successfully.";
//             }
//             return RedirectToAction(nameof(Courses));
//         }
        
//         // === APPROVE OFFERINGS (Placeholder for now) ===
//         public IActionResult ApproveOfferings()
//         {
//             var pending = _context.CourseOfferings
//                 .Include(c => c.Sections)
//                 .Where(c => c.Status == OfferingStatus.Submitted)
//                 .ToList();
//             return View(pending);
//         }
//     }
// }
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "ProgramOfficer,Admin")]
    public class ProgramOfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProgramOfficerController> _logger;
        private readonly NotificationService _notifications;

        public ProgramOfficerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ProgramOfficerController> logger, NotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notifications = notifications;
        }

        public IActionResult Index()
        {
            return View();
        }

        // === 1. ACADEMIC PERIODS (Semesters) ===
        public async Task<IActionResult> Semesters()
        {
            return View(await _context.AcademicPeriods.OrderByDescending(p => p.StartDate).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemester(string Name, string StartDate, string EndDate, bool IsActive)
        {
            _logger.LogInformation(
                "CreateSemester called. Name={Name}, StartDateRaw={StartDateRaw}, EndDateRaw={EndDateRaw}, IsActive={IsActive}",
                Name, StartDate, EndDate, IsActive);

            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["Error"] = "Semester name is required.";
                return RedirectToAction(nameof(Semesters));
            }

            if (!TryParseHtmlDate(StartDate, out var start))
            {
                TempData["Error"] = $"Invalid start date: '{StartDate}'.";
                return RedirectToAction(nameof(Semesters));
            }

            if (!TryParseHtmlDate(EndDate, out var end))
            {
                TempData["Error"] = $"Invalid end date: '{EndDate}'.";
                return RedirectToAction(nameof(Semesters));
            }

            if (end < start)
            {
                TempData["Error"] = "End date must be on or after start date.";
                return RedirectToAction(nameof(Semesters));
            }

            var period = new AcademicPeriod
            {
                Name = Name.Trim(),
                StartDate = start,
                EndDate = end,
                IsActive = IsActive
            };

            try
            {
                if (period.IsActive)
                {
                    var actives = await _context.AcademicPeriods.Where(p => p.IsActive).ToListAsync();
                    foreach (var p in actives) p.IsActive = false;
                }

                _context.AcademicPeriods.Add(period);
                await _context.SaveChangesAsync();

                _logger.LogInformation("CreateSemester succeeded. New Id={Id}", period.Id);
                TempData["Success"] = "Semester created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateSemester for Name={Name}", period.Name);
                TempData["Error"] = "Failed to create semester. See logs.";
            }

            return RedirectToAction(nameof(Semesters));
        }

        private static bool TryParseHtmlDate(string value, out DateTime date)
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;

            return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }
        [HttpPost]
        public async Task<IActionResult> SetActiveSemester(int id)
        {
            var period = await _context.AcademicPeriods.FindAsync(id);
            if (period == null) return NotFound();
            var actives = await _context.AcademicPeriods.Where(p => p.IsActive).ToListAsync();
            foreach (var p in actives)
                p.IsActive = false;
            period.IsActive = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Semesters));
        }

        // === 2. RESOURCES (Rooms, Types, Courses) ===
        public async Task<IActionResult> Resources()
        {
            var model = new ResourceManagementViewModel
            {
                RoomTypes = await _context.RoomTypes.ToListAsync(),
                Rooms = await _context.Rooms.Include(r => r.RoomType).ToListAsync(),
                Courses = await _context.Courses
                    .Where(c => c.Department == "Common")
                    .ToListAsync()
            };
            ViewBag.RoomTypes = new SelectList(model.RoomTypes, "Id", "Name");
            // Departments needed for Course creation
            // ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoomType(RoomType type)
        {
            type.Name = (type.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(type.Name))
            {
                TempData["Error"] = "Room type name is required.";
                return RedirectToAction(nameof(Resources));
            }

            var exists = await _context.RoomTypes
                .AsNoTracking()
                .AnyAsync(rt => rt.Name.ToLower() == type.Name.ToLower());

            if (exists)
            {
                TempData["Error"] = "Room type already exists.";
                return RedirectToAction(nameof(Resources));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Room type data invalid.";
                return RedirectToAction(nameof(Resources));
            }

            _context.RoomTypes.Add(type);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Room type saved.";
            return RedirectToAction(nameof(Resources));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(Room room)
        {
            room.Name = (room.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(room.Name))
            {
                TempData["Error"] = "Room name is required.";
                return RedirectToAction(nameof(Resources));
            }

            var exists = await _context.Rooms
                .AsNoTracking()
                .AnyAsync(r => r.Name.ToLower() == room.Name.ToLower());

            if (exists)
            {
                TempData["Error"] = "Room already exists.";
                return RedirectToAction(nameof(Resources));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Room data invalid.";
                return RedirectToAction(nameof(Resources));
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Room saved.";
            return RedirectToAction(nameof(Resources));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            course.Department = "Common";
            ModelState.Remove(nameof(Course.Department));

            course.Code = (course.Code ?? "").Trim();

            if (await _context.Courses.AsNoTracking().AnyAsync(c => c.Code == course.Code))
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Resources));
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = string.IsNullOrWhiteSpace(errors) ? "Course data invalid." : errors;
                return RedirectToAction(nameof(Resources));
            }

            try
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Resources));
            }

            TempData["Success"] = "Course saved.";
            return RedirectToAction(nameof(Resources));
        }

        // === 3. INSTRUCTORS & QUALIFICATIONS ===
        public async Task<IActionResult> Instructors()
        {
            // Get all instructors and their qualifications
            var instructorIds = await _userManager.GetUsersInRoleAsync("Instructor");
            var ids = instructorIds.Select(u => u.Id).ToList();

            var instructors = await _context.Users
                .Where(u => ids.Contains(u.Id))
                .Include(u => u.QualifiedCourses)
                .ThenInclude(iq => iq.Course)
                .ToListAsync();

            ViewBag.IsAvailabilityLocked = await IsAvailabilityLockedAsync();

            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Name", "Name"); // Assuming Dept stored as string on User

            var approved = instructors
                .Where(u => u.IsApproved == true)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToList();

            return View(approved);
        }

        public async Task<IActionResult> InstructorApprovals()
        {
            var instructorUsers = await _userManager.GetUsersInRoleAsync("Instructor");
            var ids = instructorUsers.Select(u => u.Id).ToList();

            var pending = await _context.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id)
                            && u.IsApproved == false
                            && u.IsSelfRegistered == true)
                .OrderByDescending(u => u.RegisteredAtUtc)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveInstructorRegistration(string instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null)
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (!await _userManager.IsInRoleAsync(user, "Instructor"))
            {
                TempData["Error"] = "User is not an instructor.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (user.IsSelfRegistered != true)
            {
                TempData["Error"] = "Only self-registered instructor requests can be approved here.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (user.IsApproved)
            {
                TempData["Success"] = "Instructor is already approved.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            user.IsApproved = true;
            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(InstructorApprovals));
            }

            TempData["Success"] = "Instructor approved.";

            await _notifications.CreateAsync(
                user.Id,
                "Instructor account approved",
                "Your instructor account has been approved. You can now log in and use the system.",
                Url.Action("Index", "Home", null, Request.Scheme),
                "Account");

            return RedirectToAction(nameof(InstructorApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectInstructorRegistration(string instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null)
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (!await _userManager.IsInRoleAsync(user, "Instructor"))
            {
                TempData["Error"] = "User is not an instructor.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (user.IsSelfRegistered != true)
            {
                TempData["Error"] = "Only self-registered instructor requests can be rejected here.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            if (user.IsApproved)
            {
                TempData["Error"] = "Approved instructors cannot be rejected here.";
                return RedirectToAction(nameof(InstructorApprovals));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(InstructorApprovals));
            }

            TempData["Success"] = "Instructor request rejected.";
            return RedirectToAction(nameof(InstructorApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveInstructor(string instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(Instructors));
            }

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null)
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(Instructors));
            }

            if (!await _userManager.IsInRoleAsync(user, "Instructor"))
            {
                TempData["Error"] = "User is not an instructor.";
                return RedirectToAction(nameof(Instructors));
            }

            if (user.IsApproved)
            {
                TempData["Success"] = "Instructor is already approved.";
                return RedirectToAction(nameof(Instructors));
            }

            user.IsApproved = true;
            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Instructors));
            }

            TempData["Success"] = "Instructor approved.";
            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectInstructor(string instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(Instructors));
            }

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null)
            {
                TempData["Error"] = "Instructor not found.";
                return RedirectToAction(nameof(Instructors));
            }

            if (!await _userManager.IsInRoleAsync(user, "Instructor"))
            {
                TempData["Error"] = "User is not an instructor.";
                return RedirectToAction(nameof(Instructors));
            }

            if (user.IsApproved)
            {
                TempData["Error"] = "Approved instructors cannot be rejected here.";
                return RedirectToAction(nameof(Instructors));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Instructors));
            }

            TempData["Success"] = "Instructor request rejected.";
            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstructor(string email, string firstName, string lastName, string department, int availableHours)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Instructors));
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = "A user with this email already exists.";
                return RedirectToAction(nameof(Instructors));
            }

            var user = new ApplicationUser
            {
                UserName = email, Email = email,
                FirstName = firstName, LastName = lastName,
                Department = department, AvailableHours = availableHours,
                IsApproved = true, EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user, "Instructor!23");
            if (result.Succeeded) await _userManager.AddToRoleAsync(user, "Instructor");

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            }
            
            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignQualification(string instructorId, int courseId)
        {
            if (!await _context.InstructorQualifications.AnyAsync(x => x.InstructorId == instructorId && x.CourseId == courseId))
            {
                _context.InstructorQualifications.Add(new InstructorCourse { InstructorId = instructorId, CourseId = courseId });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qualification assigned.";
            }
            else
            {
                TempData["Error"] = "Qualification already exists.";
            }
            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveQualification(string instructorId, int courseId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
                return RedirectToAction(nameof(Instructors));

            var existing = await _context.InstructorQualifications
                .FirstOrDefaultAsync(x => x.InstructorId == instructorId && x.CourseId == courseId);

            if (existing != null)
            {
                _context.InstructorQualifications.Remove(existing);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qualification removed.";
            }

            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInstructorAvailability(string instructorId, List<string> selectedSlots)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
                return RedirectToAction(nameof(Instructors));

            if (await IsAvailabilityLockedAsync())
            {
                TempData["Error"] = "Availability is locked because a schedule has been generated for the active term.";
                return RedirectToAction(nameof(Instructors));
            }

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null) return NotFound();

            selectedSlots ??= new List<string>();
            var normalized = NormalizeSlots(selectedSlots);
            user.AvailabilitySlots = string.Join(",", normalized);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Instructors));
            }

            TempData["Success"] = "Availability updated.";
            return RedirectToAction(nameof(Instructors));
        }

        private static IEnumerable<string> NormalizeSlots(IEnumerable<string> slots)
        {
            var allowed = GetAllowedSlots().ToHashSet(StringComparer.OrdinalIgnoreCase);

            return slots
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => allowed.Contains(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s);
        }

        private static IEnumerable<string> GetAllowedSlots()
        {
            var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var times = new[] { "AM", "PM", "Night" };

            foreach (var d in days)
            {
                foreach (var t in times)
                {
                    yield return $"{d}-{t}";
                }
            }
        }

        private async Task<bool> IsAvailabilityLockedAsync()
        {
            var active = await _context.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
            if (active == null) return false;

            var academicYear = $"{active.StartDate.Year}/{active.EndDate.Year}";
            var name = (active.Name ?? "").ToLowerInvariant();
            var semester = name.Contains("ii") ? "II" : name.Contains("summer") ? "Summer" : "I";

            var approvedPubId = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && p.Status == SchedulePublicationStatus.Approved)
                .OrderByDescending(p => p.ReviewedAtUtc)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (!approvedPubId.HasValue)
                return false;

            return await _context.ScheduleMeetings
                .AsNoTracking()
                .AnyAsync(m => m.SchedulePublicationId == approvedPubId.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInstructor(string instructorId, string email, string firstName, string lastName, string department, int availableHours)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
                return RedirectToAction(nameof(Instructors));

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Instructors));
            }

            var other = await _userManager.FindByEmailAsync(email);
            if (other != null && other.Id != user.Id)
            {
                TempData["Error"] = "A user with this email already exists.";
                return RedirectToAction(nameof(Instructors));
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Department = department;
            user.AvailableHours = availableHours;

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, email);
                if (!setEmail.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", setEmail.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Instructors));
                }

                var setUserName = await _userManager.SetUserNameAsync(user, email);
                if (!setUserName.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", setUserName.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Instructors));
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Instructors));
            }

            TempData["Success"] = "Instructor updated.";
            return RedirectToAction(nameof(Instructors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInstructor(string instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
                return RedirectToAction(nameof(Instructors));

            var user = await _userManager.FindByIdAsync(instructorId);
            if (user == null) return NotFound();

            var hasAssignments = await _context.SemesterAssignments.AsNoTracking().AnyAsync(a => a.InstructorId == instructorId);
            var hasOfferings = await _context.CourseOfferingSections.AsNoTracking().AnyAsync(s => s.InstructorId == instructorId);
            var hasMeetings = await _context.ScheduleMeetings.AsNoTracking().AnyAsync(m => m.InstructorId == instructorId);

            if (hasAssignments || hasOfferings || hasMeetings)
            {
                TempData["Error"] = "Cannot delete instructor because they are referenced by existing assignments or schedules.";
                return RedirectToAction(nameof(Instructors));
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var quals = await _context.InstructorQualifications.Where(q => q.InstructorId == instructorId).ToListAsync();
                if (quals.Any())
                {
                    _context.InstructorQualifications.RemoveRange(quals);
                    await _context.SaveChangesAsync();
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Instructors));
                }

                await tx.CommitAsync();
                TempData["Success"] = "Instructor deleted.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to delete instructor {InstructorId}", instructorId);
                TempData["Error"] = "Failed to delete instructor.";
            }

            return RedirectToAction(nameof(Instructors));
        }
        
        public async Task<IActionResult> Index1(string? academicYear = null, string? semester = null)
        {
            // Choose sensible defaults (prefer active AcademicPeriod so it matches what departments submit under)
            if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
            {
                var active = await _context.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
                if (active != null)
                {
                    var mapped = MapPeriodToYearAndSemester(active);
                    academicYear ??= mapped.AcademicYear;
                    semester ??= mapped.SemesterCode;
                }
            }

            academicYear ??= DateTime.Now.Year + "/" + (DateTime.Now.Year + 1);
            semester ??= "I";

            var offerings = await _context.CourseOfferings
                .Where(o => o.AcademicYear == academicYear && o.Semester == semester)
                .OrderBy(o => o.Department)
                .ToListAsync();

            var vm = new ProgramOfficerIndexViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                Offerings = offerings.Select(o => new ProgramOfficerOfferingListItem
                {
                    OfferingId = o.Id,
                    Department = o.Department,
                    Status = o.Status,
                    HasSubmitted = o.Status == OfferingStatus.Submitted,
                    HasFeedback = o.Status == OfferingStatus.Rejected && !string.IsNullOrWhiteSpace(o.RejectionReason)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(int sectionId, int offeringId)
        {
            var section = await _context.Sections.FirstOrDefaultAsync(s => s.Id == sectionId);
            if (section == null) return NotFound();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var gridIds = await _context.Set<ScheduleGrid>()
                    .Where(g => g.SectionId == sectionId)
                    .Select(g => g.Id)
                    .ToListAsync();

                if (gridIds.Any())
                {
                    var meetings = _context.Set<ScheduleMeeting>().Where(m => gridIds.Contains(m.ScheduleGridId));
                    _context.RemoveRange(meetings);
                }

                var grids = _context.Set<ScheduleGrid>().Where(g => g.SectionId == sectionId);
                _context.RemoveRange(grids);

                var assignments = _context.SemesterAssignments.Where(a => a.SectionId == sectionId);
                _context.RemoveRange(assignments);

                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Section deleted.";
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Failed to delete section.";
            }

            return RedirectToAction(nameof(Sections), new { offeringId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApprovedOffering(int offeringId)
        {
            var offering = await _context.CourseOfferings.FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();

            if (offering.Status != OfferingStatus.Approved)
            {
                TempData["Error"] = "Only approved offerings can be deleted here.";
                return RedirectToAction(nameof(Index1), new { academicYear = offering.AcademicYear, semester = offering.Semester });
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == offering.Department);
            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Delete generated schedule-side entities (Batches/Sections/Grids/Meetings/Assignments)
                if (dept != null)
                {
                    var sysBatches = await _context.Batches
                        .Include(b => b.Sections)
                        .Where(b => b.DepartmentId == dept.Id && b.Name.Contains(offeringToken))
                        .ToListAsync();

                    var sysSectionIds = sysBatches
                        .SelectMany(b => b.Sections)
                        .Select(s => s.Id)
                        .Distinct()
                        .ToList();

                    if (sysSectionIds.Any())
                    {
                        var gridIds = await _context.Set<ScheduleGrid>()
                            .Where(g => sysSectionIds.Contains(g.SectionId))
                            .Select(g => g.Id)
                            .ToListAsync();

                        if (gridIds.Any())
                        {
                            var meetings = _context.Set<ScheduleMeeting>().Where(m => gridIds.Contains(m.ScheduleGridId));
                            _context.RemoveRange(meetings);
                        }

                        var grids = _context.Set<ScheduleGrid>().Where(g => sysSectionIds.Contains(g.SectionId));
                        _context.RemoveRange(grids);

                        var assignments = _context.SemesterAssignments.Where(a => sysSectionIds.Contains(a.SectionId));
                        _context.RemoveRange(assignments);

                        var sections = _context.Sections.Where(s => sysSectionIds.Contains(s.Id));
                        _context.RemoveRange(sections);
                    }

                    if (sysBatches.Any())
                        _context.Batches.RemoveRange(sysBatches);
                }

                // 2) Delete offering-side entities (batches, sections, room requirements)
                var offeringSectionIds = await _context.CourseOfferingSections
                    .Where(s => s.CourseOfferingId == offeringId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (offeringSectionIds.Any())
                {
                    var reqs = await _context.SectionRoomRequirements
                        .Where(r => offeringSectionIds.Contains(r.CourseOfferingSectionId))
                        .ToListAsync();
                    if (reqs.Any())
                        _context.SectionRoomRequirements.RemoveRange(reqs);

                    var offeringSections = await _context.CourseOfferingSections
                        .Where(s => offeringSectionIds.Contains(s.Id))
                        .ToListAsync();
                    if (offeringSections.Any())
                        _context.CourseOfferingSections.RemoveRange(offeringSections);
                }

                var offeringBatches = await _context.CourseOfferingBatches.Where(b => b.CourseOfferingId == offeringId).ToListAsync();
                if (offeringBatches.Any())
                    _context.CourseOfferingBatches.RemoveRange(offeringBatches);

                var yearLevels = await _context.CourseOfferingYearLevels.Where(y => y.CourseOfferingId == offeringId).ToListAsync();
                if (yearLevels.Any())
                    _context.CourseOfferingYearLevels.RemoveRange(yearLevels);

                var yearBatches = await _context.YearBatches.Where(y => y.CourseOfferingId == offeringId).ToListAsync();
                if (yearBatches.Any())
                {
                    foreach (var yb in yearBatches)
                        yb.CourseOfferingId = null;
                }

                _context.CourseOfferings.Remove(offering);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                TempData["Success"] = "Approved offering deleted.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to delete approved offering {OfferingId}", offeringId);
                TempData["Error"] = "Failed to delete approved offering.";
            }

            return RedirectToAction(nameof(History), new { academicYear = offering.AcademicYear, semester = offering.Semester, department = offering.Department });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenApprovedOffering(int offeringId)
        {
            var offering = await _context.CourseOfferings.FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();

            if (offering.Status != OfferingStatus.Approved)
            {
                TempData["Error"] = "Only approved offerings can be reopened.";
                return RedirectToAction(nameof(Review), new { offeringId });
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == offering.Department);
            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dept != null)
                {
                    var sysBatches = await _context.Batches
                        .Include(b => b.Sections)
                        .Where(b => b.DepartmentId == dept.Id && b.Name.Contains(offeringToken))
                        .ToListAsync();

                    var sysSectionIds = sysBatches
                        .SelectMany(b => b.Sections)
                        .Select(s => s.Id)
                        .Distinct()
                        .ToList();

                    if (sysSectionIds.Any())
                    {
                        var gridIds = await _context.Set<ScheduleGrid>()
                            .Where(g => sysSectionIds.Contains(g.SectionId))
                            .Select(g => g.Id)
                            .ToListAsync();

                        if (gridIds.Any())
                        {
                            var meetings = _context.Set<ScheduleMeeting>().Where(m => gridIds.Contains(m.ScheduleGridId));
                            _context.RemoveRange(meetings);
                        }

                        var grids = _context.Set<ScheduleGrid>().Where(g => sysSectionIds.Contains(g.SectionId));
                        _context.RemoveRange(grids);

                        var assignments = _context.SemesterAssignments.Where(a => sysSectionIds.Contains(a.SectionId));
                        _context.RemoveRange(assignments);

                        var sections = _context.Sections.Where(s => sysSectionIds.Contains(s.Id));
                        _context.RemoveRange(sections);
                    }

                    if (sysBatches.Any())
                        _context.Batches.RemoveRange(sysBatches);
                }

                offering.Status = OfferingStatus.Draft;
                offering.RejectionReason = null;
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                TempData["Success"] = "Offering reopened. Department can now modify and resubmit.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to reopen approved offering {OfferingId}", offeringId);
                TempData["Error"] = "Failed to reopen offering.";
            }

            return RedirectToAction(nameof(Review), new { offeringId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReReviewRejectApprovedOffering(int offeringId, string reason)
        {
            var offering = await _context.CourseOfferings.FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();

            if (offering.Status != OfferingStatus.Approved)
            {
                TempData["Error"] = "Only approved offerings can be re-reviewed and rejected.";
                return RedirectToAction(nameof(Review), new { offeringId });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Rejection reason is required.";
                return RedirectToAction(nameof(Review), new { offeringId });
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == offering.Department);
            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dept != null)
                {
                    var sysBatches = await _context.Batches
                        .Include(b => b.Sections)
                        .Where(b => b.DepartmentId == dept.Id && b.Name.Contains(offeringToken))
                        .ToListAsync();

                    var sysSectionIds = sysBatches
                        .SelectMany(b => b.Sections)
                        .Select(s => s.Id)
                        .Distinct()
                        .ToList();

                    if (sysSectionIds.Any())
                    {
                        var gridIds = await _context.Set<ScheduleGrid>()
                            .Where(g => sysSectionIds.Contains(g.SectionId))
                            .Select(g => g.Id)
                            .ToListAsync();

                        if (gridIds.Any())
                        {
                            var meetings = _context.Set<ScheduleMeeting>().Where(m => gridIds.Contains(m.ScheduleGridId));
                            _context.RemoveRange(meetings);
                        }

                        var grids = _context.Set<ScheduleGrid>().Where(g => sysSectionIds.Contains(g.SectionId));
                        _context.RemoveRange(grids);

                        var assignments = _context.SemesterAssignments.Where(a => sysSectionIds.Contains(a.SectionId));
                        _context.RemoveRange(assignments);

                        var sections = _context.Sections.Where(s => sysSectionIds.Contains(s.Id));
                        _context.RemoveRange(sections);
                    }

                    if (sysBatches.Any())
                        _context.Batches.RemoveRange(sysBatches);
                }

                offering.Status = OfferingStatus.Rejected;
                offering.RejectionReason = reason;
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                TempData["Success"] = "Offering rejected for re-review. Department can now modify and resubmit.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to re-review reject approved offering {OfferingId}", offeringId);
                TempData["Error"] = "Failed to reject offering.";
            }

            return RedirectToAction(nameof(Review), new { offeringId });
        }

        // 2) Review a single offering (read-only ManageOffering-style view)
        public async Task<IActionResult> Review(int offeringId)
        {
            // Essentially the same data loading as CourseOfferingController.ManageOffering,
            // but WITHOUT department-role restrictions, and for PO only.
            var offering = await _context.CourseOfferings
                .Include(o => o.Sections)
                .FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();

            var batches = await _context.CourseOfferingBatches
                .Include(b => b.Sections).ThenInclude(s => s.Course)
                .Include(b => b.Sections).ThenInclude(s => s.Instructor)
                .Include(b => b.Sections).ThenInclude(s => s.SectionInstructors).ThenInclude(si => si.Instructor)
                .Where(b => b.CourseOfferingId == offeringId)
                .OrderBy(b => b.YearLevel).ThenBy(b => b.BatchName)
                .ToListAsync();

            var isArchitecture = string.Equals(offering.Department?.Trim(), "Architecture", StringComparison.OrdinalIgnoreCase);

            var modelBatches = new List<BatchViewModel>();
            foreach (var b in batches)
            {
                var bvm = new BatchViewModel
                {
                    Id = b.Id,
                    YearLevel = b.YearLevel,
                    BatchName = b.BatchName,
                    SemesterName = b.Semester,
                    CourseOfferingId = b.CourseOfferingId,
                    Status = offering.Status,
                    Courses = new List<BatchCourseViewModel>()
                };

                foreach (var s in b.Sections.OrderBy(x => x.CourseId))
                {
                    var roomReqs = await _context.SectionRoomRequirements
                        .Include(r => r.RoomType)
                        .Where(r => r.CourseOfferingSectionId == s.Id)
                        .ToListAsync();

                    var instructorName = s.Instructor?.FullName ?? "Unassigned";
                    if (isArchitecture)
                    {
                        var names = s.SectionInstructors
                            .Select(si => si.Instructor?.FullName)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList();

                        if (names.Any()) instructorName = string.Join(", ", names);
                    }

                    bvm.Courses.Add(new BatchCourseViewModel
                    {
                        Id = s.Id,
                        CourseId = s.CourseId,
                        CourseName = s.Course != null ? $"{s.Course.Code} - {s.Course.Name}" : "Unassigned",
                        InstructorName = instructorName,
                        RoomTypes = roomReqs.Select(r => r.RoomType?.Name ?? "Unknown").ToList(),
                        ContactHours = s.AssignedContactHours,
                        IsFullDay = s.IsFullDay,
                        Status = s.Period ?? "Draft"
                    });
                }

                modelBatches.Add(bvm);
            }

            var vm = new ManageOfferingViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                Status = offering.Status,
                Batches = modelBatches
            };

            return View(vm); // will use a PO-specific view
        }

        // 3) Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int offeringId)
        {
            var offering = await _context.CourseOfferings.FindAsync(offeringId);
            if (offering == null) return NotFound();
            if (offering.Status != OfferingStatus.Submitted)
                return BadRequest("Only submitted offerings can be approved.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                offering.Status = OfferingStatus.Approved;
                offering.RejectionReason = null;
                await _context.SaveChangesAsync();

                var genError = await GenerateSectionsForOfferingAsync(offering);
                if (genError != null)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = "Instructor load limit exceeded. Adjust instructor assignments before approving.";
                    return RedirectToAction(nameof(Index1), new { academicYear = offering.AcademicYear, semester = offering.Semester });
                }

                await tx.CommitAsync();

                var deptUsers = await _userManager.GetUsersInRoleAsync("Department");
                var deptUserIds = deptUsers
                    .Where(u => !string.IsNullOrWhiteSpace(u.Department)
                                && !string.IsNullOrWhiteSpace(offering.Department)
                                && string.Equals(u.Department.Trim(), offering.Department.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.Id)
                    .ToList();

                await _notifications.CreateForUsersAsync(
                    deptUserIds,
                    "Offering approved",
                    $"Your offering for {offering.AcademicYear} {offering.Semester} was approved.",
                    Url.Action("ManageOffering", "CourseOffering", new { offeringId = offering.Id }, Request.Scheme),
                    "Offering");

                TempData["Success"] = "Offering approved and sections generated.";
                return RedirectToAction(nameof(Index1), new { academicYear = offering.AcademicYear, semester = offering.Semester });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Approve failed for offering {OfferingId}", offeringId);
                TempData["Error"] = "Failed to approve offering.";
                return RedirectToAction(nameof(Index1), new { academicYear = offering.AcademicYear, semester = offering.Semester });
            }
        }

        private async Task<object?> GenerateSectionsForOfferingAsync(CourseOffering offering)
        {
            const int maxLoad = 24;

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == offering.Department);
            if (dept == null)
                return new { error = "Department record not found." };

            var deptCode = string.IsNullOrWhiteSpace(dept.Code)
                ? new string(dept.Name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant()
                : dept.Code.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(deptCode)) deptCode = "DEPT";

            var projectedLoad = await _context.SemesterAssignments
                .AsNoTracking()
                .Where(a => a.AcademicYear == offering.AcademicYear && a.Semester == offering.Semester && a.InstructorId != null)
                .Join(_context.Courses.AsNoTracking(), a => a.CourseId, c => c.Id,
                    (a, c) => new { a.InstructorId, Hours = c.LectureHours + c.LabHours })
                .GroupBy(x => x.InstructorId!)
                .Select(g => new { InstructorId = g.Key, Load = g.Sum(x => x.Hours) })
                .ToListAsync();
            var projectedLoadMap = projectedLoad.ToDictionary(x => x.InstructorId, x => x.Load);

            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            var offeringBatches = await _context.CourseOfferingBatches
                .AsNoTracking()
                .Where(b => b.CourseOfferingId == offering.Id)
                .OrderBy(b => b.YearLevel).ThenBy(b => b.BatchName)
                .ToListAsync();

            foreach (var offeringBatch in offeringBatches)
            {
                var batchName = $"{deptCode} Year {offeringBatch.YearLevel} - {offeringBatch.BatchName} {offeringToken}";
                var batch = await _context.Batches.Include(b => b.Sections)
                    .FirstOrDefaultAsync(b => b.DepartmentId == dept.Id && b.Name == batchName);

                if (batch == null)
                {
                    batch = new Batch { DepartmentId = dept.Id, Name = batchName };
                    _context.Batches.Add(batch);
                    await _context.SaveChangesAsync();
                }

                var offeringRows = await _context.CourseOfferingSections
                    .AsNoTracking()
                    .Where(s => s.OfferingBatchId == offeringBatch.Id)
                    .GroupBy(s => s.CourseId)
                    .Select(g => new
                    {
                        CourseId = g.Key,
                        InstructorId = g.Select(x => x.InstructorId).FirstOrDefault(),
                        ContactHours = g.Select(x => x.AssignedContactHours).FirstOrDefault()
                    })
                    .ToListAsync();

                if (!offeringRows.Any())
                    continue;

                var courseIds = offeringRows.Select(r => r.CourseId).Distinct().ToList();
                var courseHoursMap = await _context.Courses
                    .AsNoTracking()
                    .Where(c => courseIds.Contains(c.Id))
                    .Select(c => new { c.Id, Hours = c.LectureHours + c.LabHours })
                    .ToDictionaryAsync(x => x.Id, x => x.Hours);

                var count = offeringBatch.SectionCount < 1 ? 1 : offeringBatch.SectionCount;
                var batchToken = ExtractBatchToken(offeringBatch.BatchName);
                for (var i = 1; i <= count; i++)
                {
                    var sectionName = $"{deptCode}{offeringBatch.YearLevel}{batchToken}Sec{i}";
                    var section = await _context.Sections
                        .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.Name == sectionName);

                    if (section == null)
                    {
                        section = new Section
                        {
                            BatchId = batch.Id,
                            Name = sectionName,
                            IsExtension = offeringBatch.IsExtension,
                            NumberOfStudents = 0
                        };
                        _context.Sections.Add(section);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        section.IsExtension = offeringBatch.IsExtension;
                    }

                    var gridExists = await _context.Set<ScheduleGrid>().AnyAsync(g => g.SectionId == section.Id);
                    if (!gridExists)
                    {
                        _context.Add(new ScheduleGrid { SectionId = section.Id });
                        await _context.SaveChangesAsync();
                    }

                    foreach (var r in offeringRows)
                    {
                        var exists = await _context.SemesterAssignments.AnyAsync(a =>
                            a.SectionId == section.Id &&
                            a.AcademicYear == offering.AcademicYear &&
                            a.Semester == offering.Semester &&
                            a.CourseId == r.CourseId);

                        if (exists) continue;

                        var hours = r.ContactHours > 0
                            ? r.ContactHours
                            : (courseHoursMap.TryGetValue(r.CourseId, out var ch) ? ch : 0);

                        if (!string.IsNullOrWhiteSpace(r.InstructorId))
                        {
                            var current = projectedLoadMap.TryGetValue(r.InstructorId, out var cv) ? cv : 0;
                            if (current + hours > maxLoad)
                                return new { error = "Instructor load limit exceeded", instructorId = r.InstructorId, currentLoad = current, limit = maxLoad, projectedLoad = current + hours };

                            projectedLoadMap[r.InstructorId] = current + hours;
                        }

                        _context.SemesterAssignments.Add(new SemesterCourseAssignment
                        {
                            AcademicYear = offering.AcademicYear,
                            Semester = offering.Semester,
                            SectionId = section.Id,
                            CourseId = r.CourseId,
                            InstructorId = r.InstructorId,
                            Status = "Draft"
                        });
                    }

                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<IActionResult> History(string? academicYear = null, string? semester = null, string? department = null, int? yearLevel = null, string? batchName = null)
        {
            var offeringsQuery = _context.CourseOfferings.AsNoTracking().Where(o => o.Status == OfferingStatus.Approved);

            if (!string.IsNullOrWhiteSpace(academicYear))
                offeringsQuery = offeringsQuery.Where(o => o.AcademicYear == academicYear);
            if (!string.IsNullOrWhiteSpace(semester))
                offeringsQuery = offeringsQuery.Where(o => o.Semester == semester);
            if (!string.IsNullOrWhiteSpace(department))
                offeringsQuery = offeringsQuery.Where(o => o.Department.Contains(department));

            var offerings = await offeringsQuery.OrderByDescending(o => o.Id).ToListAsync();
            var offeringIds = offerings.Select(o => o.Id).ToList();

            var batchesQuery = _context.CourseOfferingBatches.AsNoTracking().Where(b => offeringIds.Contains(b.CourseOfferingId));
            if (yearLevel.HasValue)
                batchesQuery = batchesQuery.Where(b => b.YearLevel == yearLevel.Value);
            if (!string.IsNullOrWhiteSpace(batchName))
                batchesQuery = batchesQuery.Where(b => b.BatchName.Contains(batchName));

            var batchCounts = await batchesQuery
                .GroupBy(b => b.CourseOfferingId)
                .Select(g => new { OfferingId = g.Key, Count = g.Count() })
                .ToListAsync();
            var batchCountMap = batchCounts.ToDictionary(x => x.OfferingId, x => x.Count);

            var sysBatchNames = await _context.Batches
                .AsNoTracking()
                .Select(b => b.Name)
                .ToListAsync();
            var sysBatchNameSet = new HashSet<string>(sysBatchNames);

            var vm = new ProgramOfficerApprovedHistoryViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                Department = department,
                YearLevel = yearLevel,
                BatchName = batchName,
                Offerings = offerings.Select(o => new ProgramOfficerApprovedHistoryItem
                {
                    OfferingId = o.Id,
                    Department = o.Department,
                    AcademicYear = o.AcademicYear,
                    Semester = o.Semester,
                    BatchCount = batchCountMap.TryGetValue(o.Id, out var c) ? c : 0,
                    HasSectionsGenerated = sysBatchNameSet.Any(n => n.Contains($"({o.AcademicYear} Sem {o.Semester} Off {o.Id})"))
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetCreateSectionsPartial(int offeringId)
        {
            var offering = await _context.CourseOfferings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();
            if (offering.Status != OfferingStatus.Approved) return BadRequest("Only approved offerings can generate sections.");

            var batches = await _context.CourseOfferingBatches.AsNoTracking()
                .Where(b => b.CourseOfferingId == offeringId)
                .OrderBy(b => b.YearLevel).ThenBy(b => b.BatchName)
                .ToListAsync();

            var vm = new CreateSectionsViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                Batches = batches.Select(b => new CreateSectionsBatchRowViewModel
                {
                    OfferingBatchId = b.Id,
                    YearLevel = b.YearLevel,
                    BatchName = b.BatchName,
                    Semester = b.Semester,
                    IsExtension = b.IsExtension,
                    SectionCount = b.SectionCount
                }).ToList()
            };

            return PartialView("_CreateSectionsModal", vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSections(int offeringId)
        {
            var offering = await _context.CourseOfferings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();
            if (offering.Status != OfferingStatus.Approved) return BadRequest("Only approved offerings can generate sections.");

            var batches = await _context.CourseOfferingBatches.AsNoTracking()
                .Where(b => b.CourseOfferingId == offeringId)
                .OrderBy(b => b.YearLevel).ThenBy(b => b.BatchName)
                .ToListAsync();

            var vm = new CreateSectionsViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                Batches = batches.Select(b => new CreateSectionsBatchRowViewModel
                {
                    OfferingBatchId = b.Id,
                    YearLevel = b.YearLevel,
                    BatchName = b.BatchName,
                    Semester = b.Semester,
                    IsExtension = b.IsExtension,
                    SectionCount = b.SectionCount
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSections(CreateSectionsViewModel model)
        {
            var offering = await _context.CourseOfferings.FirstOrDefaultAsync(o => o.Id == model.OfferingId);
            if (offering == null) return NotFound();
            if (offering.Status != OfferingStatus.Approved) return BadRequest("Only approved offerings can generate sections.");

            if (model.Batches == null || !model.Batches.Any())
                return RedirectToAction(nameof(History), new { academicYear = offering.AcademicYear, semester = offering.Semester, department = offering.Department });

            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var projectedLoad = await _context.SemesterAssignments
                    .AsNoTracking()
                    .Where(a => a.AcademicYear == offering.AcademicYear && a.Semester == offering.Semester && a.InstructorId != null)
                    .Join(_context.Courses.AsNoTracking(), a => a.CourseId, c => c.Id,
                        (a, c) => new { a.InstructorId, Hours = c.LectureHours + c.LabHours })
                    .GroupBy(x => x.InstructorId!)
                    .Select(g => new { InstructorId = g.Key, Load = g.Sum(x => x.Hours) })
                    .ToListAsync();

                var projectedLoadMap = projectedLoad.ToDictionary(x => x.InstructorId, x => x.Load);
                var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

                var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == offering.Department);
                if (dept == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest("Department record not found. Please create the department and set its Code.");
                }

                var deptCode = string.IsNullOrWhiteSpace(dept.Code)
                    ? new string(dept.Name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant()
                    : dept.Code.ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(deptCode)) deptCode = "DEPT";

                const int maxLoad = 24;

                // Ensure a Batch entity exists per offering-batch (keeps Section table isolated and schedule-friendly)
                foreach (var row in model.Batches)
                {
                    var offeringBatch = await _context.CourseOfferingBatches.FirstOrDefaultAsync(b => b.Id == row.OfferingBatchId && b.CourseOfferingId == offering.Id);
                    if (offeringBatch == null)
                    {
                        await tx.RollbackAsync();
                        return BadRequest("Invalid batch.");
                    }

                    var isExtension = offeringBatch.IsExtension;

                    var batchName = $"{deptCode} Year {row.YearLevel} - {row.BatchName} {offeringToken}";
                    var batch = await _context.Batches.Include(b => b.Sections)
                        .FirstOrDefaultAsync(b => b.DepartmentId == dept.Id && b.Name == batchName);

                    if (batch == null)
                    {
                        batch = new Batch { DepartmentId = dept.Id, Name = batchName };
                        _context.Batches.Add(batch);
                        await _context.SaveChangesAsync();
                    }

                    // Gather offering rows (courses/instructors) for this offering batch
                    var offeringRows = await _context.CourseOfferingSections
                        .AsNoTracking()
                        .Where(s => s.OfferingBatchId == offeringBatch.Id)
                        .GroupBy(s => s.CourseId)
                        .Select(g => new
                        {
                            CourseId = g.Key,
                            InstructorId = g.Select(x => x.InstructorId).FirstOrDefault(),
                            ContactHours = g.Select(x => x.AssignedContactHours).FirstOrDefault()
                        })
                        .ToListAsync();

                    if (!offeringRows.Any())
                        continue;

                    var courseIds = offeringRows.Select(r => r.CourseId).Distinct().ToList();
                    var courseHoursMap = await _context.Courses
                        .AsNoTracking()
                        .Where(c => courseIds.Contains(c.Id))
                        .Select(c => new { c.Id, Hours = c.LectureHours + c.LabHours })
                        .ToDictionaryAsync(x => x.Id, x => x.Hours);

                    var count = offeringBatch.SectionCount < 1 ? 1 : offeringBatch.SectionCount;
                    var batchToken = ExtractBatchToken(row.BatchName);
                    for (var i = 1; i <= count; i++)
                    {
                        var sectionName = $"{deptCode}{row.YearLevel}{batchToken}Sec{i}";
                        var section = await _context.Sections
                            .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.Name == sectionName);

                        if (section == null)
                        {
                            section = new Section
                            {
                                BatchId = batch.Id,
                                Name = sectionName,
                                IsExtension = isExtension,
                                NumberOfStudents = 0
                            };
                            _context.Sections.Add(section);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            section.IsExtension = isExtension;
                        }

                        // Ensure exactly one schedule grid exists per section
                        var gridExists = await _context.Set<ScheduleGrid>().AnyAsync(g => g.SectionId == section.Id);
                        if (!gridExists)
                        {
                            _context.Add(new ScheduleGrid { SectionId = section.Id });
                            await _context.SaveChangesAsync();
                        }

                        // Create SemesterCourseAssignments for this academic year/semester
                        foreach (var r in offeringRows)
                        {
                            var exists = await _context.SemesterAssignments.AnyAsync(a =>
                                a.SectionId == section.Id &&
                                a.AcademicYear == offering.AcademicYear &&
                                a.Semester == offering.Semester &&
                                a.CourseId == r.CourseId);

                            if (exists) continue;

                            var hours = r.ContactHours > 0
                                ? r.ContactHours
                                : (courseHoursMap.TryGetValue(r.CourseId, out var ch) ? ch : 0);

                            if (!string.IsNullOrWhiteSpace(r.InstructorId))
                            {
                                var current = projectedLoadMap.TryGetValue(r.InstructorId, out var cv) ? cv : 0;
                                if (current + hours > maxLoad)
                                {
                                    ModelState.AddModelError(string.Empty, $"Instructor load limit exceeded for instructor '{r.InstructorId}'. Projected load: {current + hours} hrs (Limit: {maxLoad} hrs).");
                                    await tx.RollbackAsync();
                                    TempData["Error"] = "Instructor load limit exceeded. Adjust instructor assignments before generating sections.";

                                    if (isAjax)
                                        return PartialView("_CreateSectionsModal", model);

                                    return RedirectToAction(nameof(History), new { academicYear = offering.AcademicYear, semester = offering.Semester, department = offering.Department });
                                }

                                projectedLoadMap[r.InstructorId] = current + hours;
                            }

                            _context.SemesterAssignments.Add(new SemesterCourseAssignment
                            {
                                AcademicYear = offering.AcademicYear,
                                Semester = offering.Semester,
                                SectionId = section.Id,
                                CourseId = r.CourseId,
                                InstructorId = r.InstructorId,
                                Status = "Draft"
                            });
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                if (isAjax)
                {
                    return Json(new { success = true, redirectUrl = Url.Action(nameof(Sections), new { offeringId = offering.Id }) });
                }

                return RedirectToAction(nameof(Sections), new { offeringId = offering.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "CreateSections failed for offering {OfferingId}", model.OfferingId);
                TempData["Error"] = "Failed to generate sections.";

                ModelState.AddModelError(string.Empty, "Failed to generate sections.");

                if (isAjax)
                    return PartialView("_CreateSectionsModal", model);

                return RedirectToAction(nameof(History), new { academicYear = offering.AcademicYear, semester = offering.Semester, department = offering.Department });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Sections(int offeringId)
        {
            var offering = await _context.CourseOfferings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == offeringId);
            if (offering == null) return NotFound();

            var dept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Name == offering.Department);
            if (dept == null) return BadRequest("Department record not found.");

            var deptCode = string.IsNullOrWhiteSpace(dept.Code)
                ? new string(dept.Name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant()
                : dept.Code.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(deptCode)) deptCode = "DEPT";

            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            // Find the system batches created for this offering
            var batches = await _context.Batches
                .AsNoTracking()
                .Where(b => b.DepartmentId == dept.Id && b.Name.Contains(offeringToken))
                .ToListAsync();

            var batchIds = batches.Select(b => b.Id).ToList();
            var sections = await _context.Sections
                .AsNoTracking()
                .Where(s => batchIds.Contains(s.BatchId))
                .Join(_context.Batches.AsNoTracking(), s => s.BatchId, b => b.Id, (s, b) => new { s, b })
                .OrderBy(x => x.b.Name)
                .ThenBy(x => x.s.Name)
                .Select(x => new ScheduleCentral.Models.ViewModels.ProgramOfficerSectionListItem
                {
                    SectionId = x.s.Id,
                    SectionName = x.s.Name,
                    IsExtension = x.s.IsExtension,
                    BatchName = x.b.Name
                })
                .ToListAsync();

            var vm = new ScheduleCentral.Models.ViewModels.ProgramOfficerSectionsViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                Sections = sections
            };

            return View(vm);
        }

        private static string ExtractBatchToken(string batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName)) return "";

            var idx = batchName.IndexOf("batch", StringComparison.OrdinalIgnoreCase);
            var token = idx >= 0 ? batchName[(idx + 5)..].Trim() : batchName.Trim();
            token = new string(token.Where(char.IsLetterOrDigit).ToArray());
            return token;
        }

        // 4) Reject with reason
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int offeringId, string reason)
        {
            var offering = await _context.CourseOfferings.FindAsync(offeringId);
            if (offering == null) return NotFound();
            if (offering.Status != OfferingStatus.Submitted)
                return BadRequest("Only submitted offerings can be rejected.");

            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest("Rejection reason is required.");

            offering.Status = OfferingStatus.Rejected;
            offering.RejectionReason = reason;
            await _context.SaveChangesAsync();

            var deptUsers = await _userManager.GetUsersInRoleAsync("Department");
            var deptUserIds = deptUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.Department)
                            && !string.IsNullOrWhiteSpace(offering.Department)
                            && string.Equals(u.Department.Trim(), offering.Department.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Id)
                .ToList();

            await _notifications.CreateForUsersAsync(
                deptUserIds,
                "Offering rejected",
                $"Your offering for {offering.AcademicYear} {offering.Semester} was rejected. Reason: {reason}",
                Url.Action("ManageOffering", "CourseOffering", new { offeringId = offering.Id }, Request.Scheme),
                "Offering");

            return RedirectToAction(nameof(Index1), new { academicYear = offering.AcademicYear, semester = offering.Semester });
        }

        private static (string AcademicYear, string SemesterCode) MapPeriodToYearAndSemester(AcademicPeriod period)
        {
            var academicYear = $"{period.StartDate.Year}/{period.EndDate.Year}";
            var name = period.Name?.ToLowerInvariant() ?? "";

            string semesterCode;
            if (name.Contains("ii"))
                semesterCode = "II";
            else if (name.Contains("summer"))
                semesterCode = "Summer";
            else
                semesterCode = "I";

            return (academicYear, semesterCode);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, string code, string name, int creditHours, int lectureHours, int labHours)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            code = (code ?? "").Trim();

            var duplicate = await _context.Courses.AsNoTracking().AnyAsync(c => c.Code == code && c.Id != id);
            if (duplicate)
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Resources));
            }

            course.Code = code;
            course.Name = name;
            course.CreditHours = creditHours;
            course.LectureHours = lectureHours;
            course.LabHours = labHours;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course saved.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Course code already exists.";
            }

            return RedirectToAction(nameof(Resources));
        }

        [HttpPost]
        public async Task<IActionResult> EditRoom(int id, string name, int capacity, int roomTypeId)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Name = name;
            room.Capacity = capacity;
            room.RoomTypeId = roomTypeId;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Resources));
        }

        [HttpPost]
        public async Task<IActionResult> EditRoomType(int id, string name, string? description)
        {
            var type = await _context.RoomTypes.FindAsync(id);
            if (type == null) return NotFound();

            type.Name = name;
            type.Description = description;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Resources));
        }
    }
}