using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Department,Admin")]
    public class CourseCatalogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] SharedDepartmentVisibilityGroup = new[]
        {
            "Management",
            "Accounting and Finance",
            "Marketing Management"
        };

        private static IReadOnlyCollection<string> GetVisibleCourseDepartments(string? department)
        {
            var dept = (department ?? string.Empty).Trim();
            if (SharedDepartmentVisibilityGroup.Any(d => string.Equals(d, dept, System.StringComparison.OrdinalIgnoreCase)))
            {
                return SharedDepartmentVisibilityGroup.Concat(new[] { "Common" }).ToList();
            }

            return new[] { dept, "Common" };
        }

        public CourseCatalogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string?> GetDepartmentNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user.Id);
            if (managed != null) return managed.Name;
            if (!string.IsNullOrEmpty(user.Department)) return user.Department;
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var deptName = await GetDepartmentNameAsync();
            if (deptName == null) return Forbid();

            var visibleDepartments = GetVisibleCourseDepartments(deptName);

            var courses = await _context.Courses
                .Where(c => visibleDepartments.Contains(c.Department))
                .OrderBy(c => c.Code)
                .ToListAsync();

            ViewBag.DepartmentName = deptName;
            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var deptName = await GetDepartmentNameAsync();
            if (deptName == null) return Forbid();
            ViewBag.DepartmentName = deptName;
            return View(new Course());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            var deptName = await GetDepartmentNameAsync();
            if (deptName == null) return Forbid();

            course.Department = deptName;
            ModelState.Remove(nameof(Course.Department));

            course.LabHours = 0;

            course.Code = (course.Code ?? "").Trim();

            if (await _context.Courses.AsNoTracking().AnyAsync(c => c.Code == course.Code))
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = string.IsNullOrWhiteSpace(errors) ? "Course data invalid." : errors;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Course saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var deptName = await GetDepartmentNameAsync();
            if (deptName == null) return Forbid();

            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.Department != deptName) return NotFound();

            ViewBag.DepartmentName = deptName;
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course model)
        {
            var deptName = await GetDepartmentNameAsync();
            if (deptName == null) return Forbid();

            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.Department != deptName) return NotFound();

            ModelState.Remove(nameof(Course.Department));

            model.Code = (model.Code ?? "").Trim();

            if (await _context.Courses.AsNoTracking().AnyAsync(c => c.Code == model.Code && c.Id != id))
            {
                TempData["Error"] = "Course code already exists.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = string.IsNullOrWhiteSpace(errors) ? "Course data invalid." : errors;
                return RedirectToAction(nameof(Index));
            }

            course.Code = model.Code;
            course.Name = model.Name;
            course.CreditHours = model.CreditHours;
            course.LectureHours = model.LectureHours;
            course.LabHours = 0;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course saved.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Course code already exists.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}