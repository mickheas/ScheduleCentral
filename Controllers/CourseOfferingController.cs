using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Department,Admin")]
    public class CourseOfferingController : Controller
    {
        private const int MaxInstructorLoad = 24;

        private static readonly string[] SharedDepartmentVisibilityGroup = new[]
        {
            "Management",
            "Accounting and Finance",
            "Marketing Management"
        };

        private static bool IsOfferingLocked(OfferingStatus status)
            => status == OfferingStatus.Submitted || status == OfferingStatus.Approved;

        private static IReadOnlyCollection<string> GetVisibleCourseDepartments(string? department)
        {
            var dept = (department ?? string.Empty).Trim();
            if (SharedDepartmentVisibilityGroup.Any(d => string.Equals(d, dept, StringComparison.OrdinalIgnoreCase)))
            {
                return SharedDepartmentVisibilityGroup.Concat(new[] { "Common" }).ToList();
            }

            return new[] { dept, "Common" };
        }

        private sealed class InstructorLoadRow
        {
            public string InstructorId { get; set; } = "";
            public int Load { get; set; }
        }
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InstructorService _instructorService;
        private readonly ILogger<CourseOfferingController> _logger;
        private readonly NotificationService _notifications;

        public CourseOfferingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, InstructorService instructorService, ILogger<CourseOfferingController> logger, NotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _instructorService = instructorService;
            _logger = logger;
            _notifications = notifications;
        }

        public class UpdateBatchSettingsRequest
        {
            public int OfferingBatchId { get; set; }
            public bool IsExtension { get; set; }
            public int SectionCount { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBatchSettings([FromBody] UpdateBatchSettingsRequest request)
        {
            if (request == null) return BadRequest(new { error = "Missing payload" });
            if (request.SectionCount < 1) request.SectionCount = 1;

            var offeringBatch = await _context.CourseOfferingBatches
                .Include(b => b.CourseOffering)
                .FirstOrDefaultAsync(b => b.Id == request.OfferingBatchId);
            if (offeringBatch == null) return NotFound(new { error = "Batch not found" });

            var offering = offeringBatch.CourseOffering;
            if (offering == null) return BadRequest(new { error = "Offering not found" });

            if (IsOfferingLocked(offering.Status))
                return BadRequest(new { error = "Offering is locked and cannot be modified." });

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin"))
            {
                var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user!.Id);
                if (managed == null || managed.Name != offering.Department)
                    return Forbid();
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                offeringBatch.IsExtension = request.IsExtension;
                offeringBatch.SectionCount = request.SectionCount;
                await _context.SaveChangesAsync();

                var overload = await ValidateProjectedInstructorLoadAsync(offering);
                if (overload != null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(overload);
                }

                await tx.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "UpdateBatchSettings failed for OfferingBatchId={OfferingBatchId}", request.OfferingBatchId);
                return StatusCode(500, new { error = "Failed to update batch settings" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSectionDetails(int sectionId)
        {
            try
            {
                var section = await _context.CourseOfferingSections
                    .Include(s => s.RoomRequirements)
                        .ThenInclude(r => r.RoomType)
                    .Include(s => s.OfferingBatch)
                        .ThenInclude(b => b!.CourseOffering)
                    .Include(s => s.SectionInstructors)
                    .FirstOrDefaultAsync(s => s.Id == sectionId);

                if (section == null) return NotFound(new { error = "Section not found" });

                var isArchitecture = string.Equals(section.OfferingBatch?.CourseOffering?.Department?.Trim(), "Architecture", StringComparison.OrdinalIgnoreCase);

                return Json(new
                {
                    instructorId = section.InstructorId,
                    instructorIds = isArchitecture
                        ? section.SectionInstructors.Select(x => x.InstructorId).ToList()
                        : new List<string>(),
                    isFullDay = section.IsFullDay,
                    roomRequirements = section.RoomRequirements.Select(r => new
                    {
                        roomTypeId = r.RoomTypeId,
                        roomTypeName = r.RoomType != null ? r.RoomType.Name : "Unknown",
                        hoursPerWeek = r.HoursPerWeek
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error loading section details", details = ex.Message });
            }
        }

        // INDEX: show all offerings as cards (department head sees only their department)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // fetch offerings for user department (admins see all)
            // Include Batches and Sections with RoomRequirements for progress calculation
            var offeringsQuery = _context.CourseOfferings
                .Include(o => o.Batches)
                .Include(o => o.Sections)
                    .ThenInclude(s => s.RoomRequirements)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var managedDept = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user.Id);
                if (managedDept != null)
                    offeringsQuery = offeringsQuery.Where(o => o.Department == managedDept.Name);
                else if (!string.IsNullOrEmpty(user.Department))
                    offeringsQuery = offeringsQuery.Where(o => o.Department == user.Department);
            }

            var offerings = await offeringsQuery.OrderByDescending(o => o.Id).ToListAsync();

            // Build simple viewmodel with card info
            var cardVm = new List<CourseOfferingIndexCardViewModel>();
            foreach (var off in offerings)
            {
                cardVm.Add(new CourseOfferingIndexCardViewModel
                {
                    Id = off.Id,
                    Department = off.Department,
                    AcademicYear = off.AcademicYear,
                    Semester = off.Semester,
                    Status = off.Status,
                    BatchCount = off.Batches.Count,
                    ProgressPercentage = off.CalculateProgress()
                });
            }

            // For the Index view we will use the full CourseOfferingIndexViewModel (you can extend)
            var indexVm = new CourseOfferingIndexViewModel
            {
                YearPrograms = new List<YearProgramViewModel>(), // not used here; ManageOffering builds batches separately
                CourseOfferings = cardVm
            };

            // Pass cardVm via ViewBag for now
            //ViewBag.OfferingCards = cardVm;

            return View(indexVm);
        }

        // GET: Create offering (only department creates)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var managedDept = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user!.Id);

            _logger.LogInformation("GET CourseOffering.Create called. UserId={UserId}", user?.Id);

            var active = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
            if (active == null)
            {
                _logger.LogWarning("GET CourseOffering.Create blocked: no active AcademicPeriod.");
                TempData["Error"] = "No active academic period. Ask the Program Officer to create and activate a semester before creating offerings.";
                return RedirectToAction(nameof(Index));
            }

            var mapped = MapPeriodToYearAndSemester(active);

            var vm = new CourseOfferingCreateViewModel
            {
                Department = !User.IsInRole("Admin") ? (managedDept?.Name ?? user!.Department ?? "Unassigned") : string.Empty,
                AcademicYear = mapped.AcademicYear,
                Semester = mapped.SemesterCode
            };

            _logger.LogInformation(
                "Create offering form prepared. Dept={Dept}, AcademicYear={Year}, Semester={Sem}",
                vm.Department, vm.AcademicYear, vm.Semester);

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Department,Admin")]
        public async Task<IActionResult> MyFeedback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<CourseOffering> q = _context.CourseOfferings.AsNoTracking();

            if (!User.IsInRole("Admin"))
            {
                var managedDept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.HeadId == user.Id);
                var deptName = managedDept?.Name ?? user.Department;
                if (string.IsNullOrWhiteSpace(deptName)) return Forbid();
                q = q.Where(o => o.Department == deptName);
            }

            var offering = await q
                .Where(o => o.Status == OfferingStatus.Rejected && !string.IsNullOrWhiteSpace(o.RejectionReason))
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (offering == null)
            {
                TempData["Error"] = "No feedback available.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Feedback), new { offeringId = offering.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseOfferingCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            var managedDept = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user!.Id);
            var deptName = model.Department;

            _logger.LogInformation(
                "POST CourseOffering.Create called. UserId={UserId}, DeptFromModel={Dept}",
                user?.Id, deptName);

            var active = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
            if (active == null)
            {
                _logger.LogWarning("POST CourseOffering.Create blocked: no active AcademicPeriod.");
                TempData["Error"] = "No active academic period. Ask the Program Officer to create and activate a semester before creating offerings.";
                return RedirectToAction(nameof(Index));
            }
            var (academicYear, semesterCode) = MapPeriodToYearAndSemester(active);

            if (!User.IsInRole("Admin")) deptName = managedDept?.Name ?? user!.Department ?? model.Department;

            _logger.LogInformation(
                "Resolved DeptName={DeptName}. Using AcademicYear={Year}, Semester={Sem}",
                deptName, academicYear, semesterCode);

            // Enforce: only one offering per department per academic year + semester
            var duplicateExists = await _context.CourseOfferings
                .AnyAsync(o => o.Department == deptName
                               && o.AcademicYear == academicYear
                               && o.Semester == semesterCode);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate CourseOffering creation blocked. Dept={Dept}, Year={Year}, Sem={Sem}",
                    deptName, academicYear, semesterCode);

                TempData["Error"] = "An offering for this department and semester already exists.";
                return RedirectToAction(nameof(Index));
            }

            var offering = new CourseOffering
            {
                Department = deptName,
                AcademicYear = academicYear,
                Semester = semesterCode,
                Status = OfferingStatus.Creation
            };

            _context.CourseOfferings.Add(offering);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CourseOffering created. OfferingId={OfferingId}", offering.Id);

            TempData["Success"] = "Course offering created. Now click Manage to add batches and courses.";
            return RedirectToAction(nameof(ManageOffering), new { offeringId = offering.Id });
        }

        // MANAGE OFFERING: show batches accordion and their course tables
        [HttpGet]
        public async Task<IActionResult> ManageOffering(int offeringId)
        {
            var offering = await _context.CourseOfferings
                .Include(o => o.Sections)
                .FirstOrDefaultAsync(o => o.Id == offeringId);

            if (offering == null) return NotFound();

            // Security: only department head or admin can manage
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin"))
            {
                var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user!.Id);
                if (managed == null || managed.Name != offering.Department)
                    return Forbid();
            }

            var isArchitecture = string.Equals(offering.Department?.Trim(), "Architecture", StringComparison.OrdinalIgnoreCase);

            // Load batches for this offering
            var batches = await _context.CourseOfferingBatches
                .Include(b => b.Sections)
                    .ThenInclude(s => s.Course)
                .Include(b => b.Sections)
                    .ThenInclude(s => s.Instructor)
                .Include(b => b.Sections)
                    .ThenInclude(s => s.SectionInstructors)
                        .ThenInclude(si => si.Instructor)
                .Where(b => b.CourseOfferingId == offeringId)
                .OrderBy(b => b.YearLevel).ThenBy(b => b.BatchName)
                .ToListAsync();

            // Build the ManageOffering view model (reuse BatchViewModel)
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
                    IsExtension = b.IsExtension,
                    SectionCount = b.SectionCount,
                    Courses = new List<BatchCourseViewModel>()
                };

                foreach (var s in b.Sections.OrderBy(x => x.CourseId))
                {
                    var roomReqs = await _context.SectionRoomRequirements.Include(r => r.RoomType)
                                            .Where(r => r.CourseOfferingSectionId == s.Id).ToListAsync();

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

            // ViewBag populates dropdowns & lists for modals (AddBatch & AddCourse)
            ViewBag.Offering = offering;

            var visibleDepartments = GetVisibleCourseDepartments(offering.Department);
            var visibleCourseQuery = _context.Courses
                .AsNoTracking()
                .Where(c => visibleDepartments.Contains(c.Department))
                .OrderBy(c => c.Code);

            ViewBag.Courses = new SelectList(await visibleCourseQuery.ToListAsync(), "Id", "Code");
            ViewBag.AllCourses = new SelectList(await visibleCourseQuery.Select(c => new
            {
                Id = c.Id,
                DisplayText = $"{c.Code} - {c.Name}"
            }).ToListAsync(), "Id", "DisplayText");
            var instructors = await _instructorService.GetAvailableInstructorsAsync(offering.Semester, offering.AcademicYear);
            ViewBag.Instructors = instructors.Select(i => new SelectListItem { Value = i.Id, Text = i.DisplayText, Disabled = i.IsOverloaded });
            ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(rt => rt.Name).ToListAsync();

            // Build a typed view model for ManageOffering
            var manageVm = new ManageOfferingViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                Status = offering.Status,
                IsLocked = IsOfferingLocked(offering.Status),
                Batches = modelBatches
            };

            return View(manageVm);
        }
        // // GET: /CourseOffering/GetCourseDetails?courseId=5&semester=I
        // [HttpGet]
        // public async Task<IActionResult> GetCourseDetails(int courseId, string semester)
        // {
        //     // 1. Get Course Info
        //     var course = await _context.Courses.FindAsync(courseId);
        //     if (course == null) return NotFound();
        //     // 2. Get Qualified Instructors for this course
        //     var qualifiedInstructorIds = await _context.InstructorQualifications
        //         .Where(iq => iq.CourseId == courseId)
        //         .Select(iq => iq.InstructorId)
        //         .ToListAsync();
        //     // 3. Get the instructor details and load status using the service
        //     var allInstructorsWithLoad = await _instructorService.GetAvailableInstructorsAsync(semester);

        //     // QUICK SAFETY: ensure both sides are strings and compare correctly
        //     var qualSet = new HashSet<string>(qualifiedInstructorIds ?? new List<string>());


        //     var qualifiedInstructors = allInstructorsWithLoad
        //         .Where(i => qualSet.Contains(i.Id))
        //         .Select(i => new
        //         {
        //             id = i.Id,
        //             name = i.DisplayText, // This includes FullName and potentially Department/Load
        //             isOverloaded = i.IsOverloaded // Assuming InstructorService populates this flag
        //         })
        //         .OrderBy(i => i.isOverloaded) // Put non-overloaded instructors first
        //         .ToList();
        //     return Json(new
        //     {
        //         contactHours = course.ContactHours,
        //         instructors = qualifiedInstructors
        //     });
        // }

        [HttpGet]
        public async Task<IActionResult> GetCourseDetails(int courseId, string semester, string? academicYear = null)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId);
                
                if (course == null)
                    return NotFound(new { error = "Course not found" });

                // Get qualified instructors for this course
                var qualifiedInstructors = await _instructorService.GetQualifiedInstructorsForCourse(courseId, semester, academicYear);

                return Json(new
                {
                    contactHours = course.ContactHours,
                    qualifiedInstructors = qualifiedInstructors.Select(i => new
                    {
                        id = i.Id,
                        name = i.FullName,
                        currentLoad = i.CurrentLoad,
                        maxLoad = MaxInstructorLoad,
                        isOverloaded = i.IsOverloaded
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching course details", details = ex.Message });
            }
        }
        // POST: Add Batch (modal)
        [HttpPost]
        public async Task<IActionResult> AddBatch(int offeringId, int yearLevel, string semester, string? batchName = null)
        {
            var offering = await _context.CourseOfferings.FindAsync(offeringId);
            if (offering == null) return NotFound("Offering not found");

            if (IsOfferingLocked(offering.Status))
                return Json(new { success = false, error = "Offering is locked and cannot be modified." });

            // enforce the offering index (dept restriction)
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin"))
            {
                var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user!.Id);
                if (managed == null || managed.Name != offering.Department) return Forbid();
            }

            semester = offering.Semester;

            // Auto-generate BatchName as "Batch I" if not provided
            var suggestedName = batchName?.Trim();
            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                var existingNames = await _context.CourseOfferingBatches
                    .Where(b => b.CourseOfferingId == offeringId && b.YearLevel == yearLevel)
                    .Select(b => b.BatchName)
                    .ToListAsync();

                var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

                var next = existingNames.Count + 1;
                while (true)
                {
                    var roman = ToRoman(next);
                    var candidate = $"Batch {roman}";
                    if (!existingSet.Contains(candidate))
                    {
                        suggestedName = candidate;
                        break;
                    }

                    next++;
                }
            }

            var exists = await _context.CourseOfferingBatches.AnyAsync(b =>
                b.CourseOfferingId == offeringId &&
                b.YearLevel == yearLevel &&
                b.BatchName.ToLower() == suggestedName.ToLower());
            if (exists)
            {
                return Json(new { success = false, error = "Duplicate batch. This year level + batch name already exists for the department." });
            }

            var batch = new CourseOfferingBatch
            {
                CourseOfferingId = offeringId,
                YearLevel = yearLevel,
                Semester = semester,
                BatchName = suggestedName
            };

            _context.CourseOfferingBatches.Add(batch);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, error = "Duplicate batch. This year level + batch name already exists for the department." });
            }

            return Json(new { success = true, batchId = batch.Id, name = batch.BatchName, yearLevel = batch.YearLevel });
        }


        [HttpPost]
        public async Task<IActionResult> SaveCourseSection([FromBody] SaveSectionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Missing payload" });

                // Validate batch and offering
                var batch = await _context.CourseOfferingBatches
                    .Include(b => b.CourseOffering)
                    .FirstOrDefaultAsync(b => b.Id == request.BatchId);
                if (batch == null)
                    return NotFound(new { error = "Batch not found" });

                if (batch.CourseOffering == null)
                    return BadRequest(new { error = "Offering not found" });

                if (IsOfferingLocked(batch.CourseOffering.Status))
                    return BadRequest(new { error = "Offering is locked and cannot be modified." });

                var visibleDepartments = GetVisibleCourseDepartments(batch.CourseOffering.Department);
                var courseIsVisible = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == request.CourseId && visibleDepartments.Contains(c.Department));

                if (!courseIsVisible)
                    return BadRequest(new { error = "Selected course is not available for this department." });

                var isArchitecture = string.Equals(batch.CourseOffering.Department?.Trim(), "Architecture", StringComparison.OrdinalIgnoreCase);

                await using var tx = await _context.Database.BeginTransactionAsync();

                CourseOfferingSection section;

                if (request.SectionId > 0)
                {
                    // Update existing section
                    section = await _context.CourseOfferingSections
                        .Include(s => s.SectionInstructors)
                        .FirstOrDefaultAsync(s => s.Id == request.SectionId);

                    if (section == null)
                        return NotFound(new { error = "Section not found" });

                    var duplicateExists = await _context.CourseOfferingSections
                        .AsNoTracking()
                        .AnyAsync(s => s.OfferingBatchId == batch.Id && s.CourseId == request.CourseId && s.Id != request.SectionId);

                    if (duplicateExists)
                        return BadRequest(new { error = "This course is already booked for the selected batch." });

                    section.CourseId = request.CourseId;
                    section.InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? null : request.InstructorId;
                    section.AssignedContactHours = request.TotalHours;
                    section.OfferingBatchId = batch.Id;
                    section.CourseOfferingId = batch.CourseOfferingId;
                    section.IsFullDay = isArchitecture && request.IsFullDay;
                }
                else
                {
                    // Create new section (similar to AddCourseToBatch)
                    var duplicateExists = await _context.CourseOfferingSections
                        .AsNoTracking()
                        .AnyAsync(s => s.OfferingBatchId == batch.Id && s.CourseId == request.CourseId);

                    if (duplicateExists)
                        return BadRequest(new { error = "This course is already booked for the selected batch." });

                    section = new CourseOfferingSection
                    {
                        CourseOfferingId = batch.CourseOfferingId,
                        OfferingBatchId = batch.Id,
                        CourseId = request.CourseId,
                        YearLevel = $"{Ordinal(batch.YearLevel)}",
                        SectionName = $"{Ordinal(batch.YearLevel)} - {batch.BatchName}",
                        ProgramType = "Regular",
                        InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? null : request.InstructorId,
                        AssignedContactHours = request.TotalHours,
                        IsFullDay = isArchitecture && request.IsFullDay
                    };

                    _context.CourseOfferingSections.Add(section);
                }

                await _context.SaveChangesAsync();

                if (isArchitecture)
                {
                    var selected = (request.InstructorIds ?? new List<string>())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (request.SectionId > 0)
                    {
                        if (section.SectionInstructors.Any())
                            _context.CourseOfferingSectionInstructors.RemoveRange(section.SectionInstructors);
                    }
                    else
                    {
                        section.SectionInstructors = new List<CourseOfferingSectionInstructor>();
                    }

                    foreach (var instructorId in selected)
                    {
                        _context.CourseOfferingSectionInstructors.Add(new CourseOfferingSectionInstructor
                        {
                            CourseOfferingSectionId = section.Id,
                            InstructorId = instructorId
                        });
                    }

                    section.InstructorId = selected.FirstOrDefault();
                    await _context.SaveChangesAsync();
                }

                // Remove existing room requirements
                var existingReqs = await _context.SectionRoomRequirements
                    .Where(r => r.CourseOfferingSectionId == section.Id)
                    .ToListAsync();
                if (existingReqs.Any())
                    _context.SectionRoomRequirements.RemoveRange(existingReqs);

                // Add new room requirements
                if (request.RoomRequirements != null && request.RoomRequirements.Any())
                {
                    foreach (var req in request.RoomRequirements)
                    {
                        _context.SectionRoomRequirements.Add(new SectionRoomRequirement
                        {
                            CourseOfferingSectionId = section.Id,
                            RoomTypeId = req.RoomTypeId,
                            HoursPerWeek = req.Hours
                        });
                    }
                }

                await _context.SaveChangesAsync();

                var overload = await ValidateProjectedInstructorLoadAsync(batch.CourseOffering);
                if (overload != null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(overload);
                }

                await tx.CommitAsync();
                return Json(new { success = true, sectionId = section.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error saving section", details = ex.Message });
            }
        }

        private async Task<object?> ValidateProjectedInstructorLoadAsync(CourseOffering offering)
        {
            var dept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Name == offering.Department);
            var offeringToken = $"({offering.AcademicYear} Sem {offering.Semester} Off {offering.Id})";

            var isArchitecture = string.Equals(offering.Department?.Trim(), "Architecture", StringComparison.OrdinalIgnoreCase);

            List<int> offeringSectionIds = new();
            if (dept != null)
            {
                var sysBatchIds = await _context.Batches
                    .AsNoTracking()
                    .Where(b => b.DepartmentId == dept.Id && b.Name.Contains(offeringToken))
                    .Select(b => b.Id)
                    .ToListAsync();

                if (sysBatchIds.Any())
                {
                    offeringSectionIds = await _context.Sections
                        .AsNoTracking()
                        .Where(s => sysBatchIds.Contains(s.BatchId))
                        .Select(s => s.Id)
                        .ToListAsync();
                }
            }

            var baseLoads = await _context.SemesterAssignments
                .AsNoTracking()
                .Where(a => a.AcademicYear == offering.AcademicYear && a.Semester == offering.Semester && a.InstructorId != null)
                .Where(a => !offeringSectionIds.Contains(a.SectionId))
                .Join(_context.Courses.AsNoTracking(), a => a.CourseId, c => c.Id,
                    (a, c) => new { InstructorId = a.InstructorId!, Hours = c.LectureHours + c.LabHours })
                .GroupBy(x => x.InstructorId)
                .Select(g => new { InstructorId = g.Key, Load = g.Sum(x => x.Hours) })
                .ToListAsync();

            var baseMap = baseLoads.ToDictionary(x => x.InstructorId, x => x.Load);

            List<InstructorLoadRow> plannedLoads;
            if (isArchitecture)
            {
                plannedLoads = await _context.CourseOfferingSectionInstructors
                    .AsNoTracking()
                    .Join(_context.CourseOfferingSections.AsNoTracking(), si => si.CourseOfferingSectionId, s => s.Id, (si, s) => new { si, s })
                    .Where(x => x.s.CourseOfferingId == offering.Id)
                    .Join(_context.CourseOfferingBatches.AsNoTracking(), xs => xs.s.OfferingBatchId, b => b.Id, (xs, b) => new { xs.si, xs.s, b })
                    .Join(_context.Courses.AsNoTracking(), xsb => xsb.s.CourseId, c => c.Id, (xsb, c) => new
                    {
                        InstructorId = xsb.si.InstructorId,
                        Hours = (xsb.s.AssignedContactHours > 0 ? xsb.s.AssignedContactHours : (c.LectureHours + c.LabHours)) * (xsb.b.SectionCount > 0 ? xsb.b.SectionCount : 1)
                    })
                    .GroupBy(x => x.InstructorId)
                    .Select(g => new InstructorLoadRow { InstructorId = g.Key, Load = g.Sum(x => x.Hours) })
                    .ToListAsync();
            }
            else
            {
                plannedLoads = await _context.CourseOfferingSections
                    .AsNoTracking()
                    .Where(s => s.CourseOfferingId == offering.Id && s.InstructorId != null)
                    .Join(_context.CourseOfferingBatches.AsNoTracking(), s => s.OfferingBatchId, b => b.Id, (s, b) => new { s, b })
                    .Join(_context.Courses.AsNoTracking(), sb => sb.s.CourseId, c => c.Id, (sb, c) => new
                    {
                        InstructorId = sb.s.InstructorId!,
                        Hours = (sb.s.AssignedContactHours > 0 ? sb.s.AssignedContactHours : (c.LectureHours + c.LabHours)) * (sb.b.SectionCount > 0 ? sb.b.SectionCount : 1)
                    })
                    .GroupBy(x => x.InstructorId)
                    .Select(g => new InstructorLoadRow { InstructorId = g.Key, Load = g.Sum(x => x.Hours) })
                    .ToListAsync();
            }

            foreach (var p in plannedLoads)
            {
                var baseLoad = baseMap.TryGetValue(p.InstructorId, out var bl) ? bl : 0;
                var projected = baseLoad + p.Load;
                if (projected > MaxInstructorLoad)
                {
                    return new
                    {
                        error = "Instructor load limit exceeded",
                        instructorId = p.InstructorId,
                        currentLoad = baseLoad,
                        limit = MaxInstructorLoad,
                        projectedLoad = projected
                    };
                }
            }

            return null;
        }

        // Department: show rejection feedback
        [HttpGet]
        [Authorize(Roles = "Department,Admin")]
        public async Task<IActionResult> Feedback(int offeringId)
        {
            var offering = await _context.CourseOfferings.FindAsync(offeringId);
            if (offering == null) return NotFound();

            if (offering.Status != OfferingStatus.Rejected || string.IsNullOrWhiteSpace(offering.RejectionReason))
                return RedirectToAction(nameof(ManageOffering), new { offeringId });

            var vm = new OfferingFeedbackViewModel
            {
                OfferingId = offering.Id,
                Department = offering.Department,
                AcademicYear = offering.AcademicYear,
                Semester = offering.Semester,
                RejectionReason = offering.RejectionReason!
            };

            return View(vm);
        }

        // Department: acknowledge feedback and go back to editable state
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Department,Admin")]
        public async Task<IActionResult> AcknowledgeFeedback(int offeringId)
        {
            var offering = await _context.CourseOfferings.FindAsync(offeringId);
            if (offering == null) return NotFound();

            // Move from Rejected → Draft (editable) but keep the reason visible
            if (offering.Status == OfferingStatus.Rejected)
            {
                offering.Status = OfferingStatus.Draft;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageOffering), new { offeringId });
        }

        // Helper: create roman numerals for batch naming
        private static string ToRoman(int number)
        {
            if (number < 1) return number.ToString();
            var map = new[] {
                new { Value = 1000, Numeral = "M" }, new { Value = 900, Numeral = "CM" }, new { Value = 500, Numeral = "D" },
                new { Value = 400, Numeral = "CD" }, new { Value = 100, Numeral = "C" }, new { Value = 90, Numeral = "XC" },
                new { Value = 50, Numeral = "L" }, new { Value = 40, Numeral = "XL" }, new { Value = 10, Numeral = "X" },
                new { Value = 9, Numeral = "IX" }, new { Value = 5, Numeral = "V" }, new { Value = 4, Numeral = "IV" },
                new { Value = 1, Numeral = "I" }
            };
            var result = "";
            foreach (var m in map)
            {
                while (number >= m.Value)
                {
                    result += m.Numeral;
                    number -= m.Value;
                }
            }
            return result;
        }

        // AJAX: Add course to batch
        [HttpPost]
        public async Task<IActionResult> AddCourseToBatch([FromBody] AddCourseToBatchDto dto)
        {
            if (dto == null) return BadRequest(new { error = "Payload required" });

            var batch = await _context.CourseOfferingBatches.Include(b => b.CourseOffering).FirstOrDefaultAsync(b => b.Id == dto.BatchId);
            if (batch == null) return NotFound(new { error = "Batch not found" });

            if (batch.CourseOffering == null) return BadRequest(new { error = "Offering not found" });

            if (IsOfferingLocked(batch.CourseOffering.Status))
                return BadRequest(new { error = "Offering is locked and cannot be modified." });

            var visibleDepartments = GetVisibleCourseDepartments(batch.CourseOffering.Department);
            var courseIsVisible = await _context.Courses
                .AsNoTracking()
                .AnyAsync(c => c.Id == dto.CourseId && visibleDepartments.Contains(c.Department));

            if (!courseIsVisible)
                return BadRequest(new { error = "Selected course is not available for this department." });

            var duplicateExists = await _context.CourseOfferingSections
                .AsNoTracking()
                .AnyAsync(s => s.OfferingBatchId == batch.Id && s.CourseId == dto.CourseId);

            if (duplicateExists)
                return BadRequest(new { error = "This course is already booked for the selected batch." });

            var course = await _context.Courses.FindAsync(dto.CourseId);
            if (course == null) return NotFound(new { error = "Course not found" });

            var multiplier = batch.SectionCount > 0 ? batch.SectionCount : 1;
            var newHours = course.ContactHours * multiplier;

            // instructor availability check
            if (!string.IsNullOrEmpty(dto.InstructorId))
            {
                var ok = await _instructorService.CheckAvailability(
                    dto.InstructorId,
                    newHours,
                    batch.Semester,
                    batch.CourseOffering?.AcademicYear);
                if (!ok) return BadRequest(new { error = "Instructor not available (exceeds available hours)" });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            // create a new CourseOfferingSection linked to batch AND to course offering (for compatibility)
            var section = new CourseOfferingSection
            {
                CourseOfferingId = batch.CourseOfferingId,
                OfferingBatchId = batch.Id,
                CourseId = dto.CourseId,
                YearLevel = $"{Ordinal(batch.YearLevel)}",
                SectionName = $"{Ordinal(batch.YearLevel)} - {batch.BatchName}",
                ProgramType = "Regular", // Phase 1 keep default
                InstructorId = dto.InstructorId,
                AssignedContactHours = course.ContactHours
            };

            _context.CourseOfferingSections.Add(section);
            await _context.SaveChangesAsync();

            // Add room-type requirements
            if (dto.RoomTypeIds != null && dto.RoomTypeIds.Any())
            {
                foreach (var rtId in dto.RoomTypeIds)
                {
                    int hours = 0;
                    if (dto.HoursPerRoomType != null && dto.HoursPerRoomType.TryGetValue(rtId, out var hv)) hours = hv;
                    if (hours <= 0) continue;

                    _context.SectionRoomRequirements.Add(new SectionRoomRequirement
                    {
                        CourseOfferingSectionId = section.Id,
                        RoomTypeId = rtId,
                        HoursPerWeek = hours
                    });
                }
                await _context.SaveChangesAsync();
            }

            var overload = await ValidateProjectedInstructorLoadAsync(batch.CourseOffering);
            if (overload != null)
            {
                await tx.RollbackAsync();
                return BadRequest(overload);
            }

            await tx.CommitAsync();

            return Json(new { success = true, id = section.Id });
        }

        // Delete Batch (ajax)
        [HttpPost]
        public async Task<IActionResult> DeleteBatch(int batchId)
        {
            var batch = await _context.CourseOfferingBatches
            .Include(b => b.Sections)
            .Include(b => b.CourseOffering)
            .FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null) return NotFound();

            if (batch.CourseOffering != null && IsOfferingLocked(batch.CourseOffering.Status))
                return BadRequest(new { error = "Offering is locked and cannot be modified." });

            // delete related section room reqs first (FK safety)
            var sectionIds = batch.Sections.Select(s => s.Id).ToList();
            var reqs = await _context.SectionRoomRequirements.Where(r => sectionIds.Contains(r.CourseOfferingSectionId)).ToListAsync();
            if (reqs.Any()) _context.SectionRoomRequirements.RemoveRange(reqs);

            if (batch.Sections.Any()) _context.CourseOfferingSections.RemoveRange(batch.Sections);

            _context.CourseOfferingBatches.Remove(batch);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Delete Course row (ajax)
        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int sectionId)
        {
            var section = await _context.CourseOfferingSections
            .Include(s => s.RoomRequirements)
            .Include(s => s.CourseOffering)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
            if (section == null) return NotFound();

            if (section.CourseOffering != null && IsOfferingLocked(section.CourseOffering.Status))
                return BadRequest(new { error = "Offering is locked and cannot be modified." });

            var reqs = await _context.SectionRoomRequirements.Where(r => r.CourseOfferingSectionId == sectionId).ToListAsync();
            if (reqs.Any()) _context.SectionRoomRequirements.RemoveRange(reqs);

            _context.CourseOfferingSections.Remove(section);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: Delete full course offering (used by Delete view and delete modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offering = await _context.CourseOfferings
                .Include(o => o.Batches)
                    .ThenInclude(b => b.Sections)
                        .ThenInclude(s => s.RoomRequirements)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offering == null)
            {
                // If already gone, just go back to index
                TempData["Success"] = "Course offering was already removed.";
                return RedirectToAction(nameof(Index));
            }

            if (IsOfferingLocked(offering.Status))
            {
                TempData["Error"] = "This course offering is locked and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            // Security: only department head for this department or admin may delete
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!User.IsInRole("Admin"))
            {
                var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user.Id);
                if (managed == null || managed.Name != offering.Department)
                    return Forbid();
            }

            // Collect all related section IDs for room requirement cleanup
            var allSections = offering.Batches
                .SelectMany(b => b.Sections)
                .ToList();

            var sectionIds = allSections.Select(s => s.Id).ToList();
            if (sectionIds.Any())
            {
                var reqs = await _context.SectionRoomRequirements
                    .Where(r => sectionIds.Contains(r.CourseOfferingSectionId))
                    .ToListAsync();
                if (reqs.Any())
                {
                    _context.SectionRoomRequirements.RemoveRange(reqs);
                }
            }

            if (allSections.Any())
            {
                _context.CourseOfferingSections.RemoveRange(allSections);
            }

            if (offering.Batches.Any())
            {
                _context.CourseOfferingBatches.RemoveRange(offering.Batches);
            }

            _context.CourseOfferings.Remove(offering);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course offering deleted successfully.";
            return RedirectToAction(nameof(Index));
        }


        // Submit offering to PO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int offeringId, int? id)
        {
            var resolvedOfferingId = offeringId > 0 ? offeringId : (id ?? 0);
            if (resolvedOfferingId <= 0) return BadRequest("Missing offeringId.");

            var offering = await _context.CourseOfferings.FindAsync(resolvedOfferingId);
            if (offering == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!User.IsInRole("Admin"))
            {
                var managed = await _context.Departments.FirstOrDefaultAsync(d => d.HeadId == user.Id);
                if (managed == null || managed.Name != offering.Department)
                    return Forbid();
            }

            if (offering.Status != OfferingStatus.Creation &&
                offering.Status != OfferingStatus.Draft &&
                offering.Status != OfferingStatus.Rejected)
            {
                return BadRequest("Only draft offerings can be submitted.");
            }

            // simple validation: must have at least one batch and one section
            var batchCount = await _context.CourseOfferingBatches.CountAsync(b => b.CourseOfferingId == resolvedOfferingId);
            if (batchCount == 0) return BadRequest("Please add at least one batch before submitting.");
            var sectionCount = await _context.CourseOfferingSections.CountAsync(s => s.CourseOfferingId == resolvedOfferingId);
            if (sectionCount == 0) return BadRequest("Please add course rows before submitting.");

            offering.Status = OfferingStatus.Submitted;
            offering.RejectionReason = null;
            await _context.SaveChangesAsync();

            await _notifications.CreateForRolesAsync(
                new[] { "ProgramOfficer", "Admin", "TopManagement" },
                "Offering submitted",
                $"{offering.Department} submitted an offering for {offering.AcademicYear} {offering.Semester}.",
                Url.Action("Index1", "ProgramOfficer", new { academicYear = offering.AcademicYear, semester = offering.Semester }, Request.Scheme),
                "Offering");

            return RedirectToAction(nameof(Index));
        }

        // InstructorLoad report
        public async Task<IActionResult> InstructorLoad(string? semester = null)
        {
            semester ??= "I";
            var list = await _instructorService.GetAvailableInstructorsAsync(semester);
            return View(list);
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
        
        // Helper: convert numeric to ordinal display (1 => "1st", 2 => "2nd")
        private static string Ordinal(int n)
        {
            if (n <= 0) return n.ToString();
            if (n % 100 >= 11 && n % 100 <= 13) return $"{n}th";
            return (n % 10) switch
            {
                1 => $"{n}st",
                2 => $"{n}nd",
                3 => $"{n}rd",
                _ => $"{n}th"
            };
        }
    }
    public class SaveSectionRequest
    {
        public int SectionId { get; set; }
        public int OfferingId { get; set; }
        public int BatchId { get; set; }
        public int CourseId { get; set; }
        public string? InstructorId { get; set; }
        public List<string> InstructorIds { get; set; } = new();
        public int TotalHours { get; set; }
        public bool IsFullDay { get; set; }
        public List<RoomRequirementDto> RoomRequirements { get; set; } = new();
        
    }

    public class RoomRequirementDto
    {
        public int RoomTypeId { get; set; }
        public int Hours { get; set; }
    }
    // Small internal VM used by the Index card (keeps it local)
    public class CourseOfferingIndexCardViewModel
    {
        public int Id { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public OfferingStatus Status { get; set; }
        public int BatchCount { get; set; }
        public int ProgressPercentage { get; set; }
    }

    // ManageOffering view model (simple)
    public class ManageOfferingViewModel
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public OfferingStatus Status { get; set; }
        public bool IsLocked { get; set; }
        public List<BatchViewModel> Batches { get; set; } = new List<BatchViewModel>();
    }
}
