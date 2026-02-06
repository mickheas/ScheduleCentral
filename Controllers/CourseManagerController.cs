using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Department,Admin,ProgramOfficer")]
    public class CourseManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. DASHBOARD: List Batches to Manage
        public async Task<IActionResult> Index()
        {
            var batches = await _context.Batches
                .Include(b => b.Department)
                .Include(b => b.Sections)
                .ToListAsync();
            return View(batches);
        }

        // 2. QUALIFICATIONS: Link Instructor to Course
        public async Task<IActionResult> ManageQualifications(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var qualifications = await _context.InstructorQualifications
                .Include(iq => iq.Instructor)
                .Where(iq => iq.CourseId == courseId)
                .Select(iq => new QualifiedInstructorViewModel
                {
                    Id = iq.Id,
                    InstructorName = iq.Instructor.FullName,
                    Department = iq.Instructor.Department ?? "General",
                    Priority = iq.Priority
                }).ToListAsync();

            // Get all instructors for dropdown
            var allInstructors = await _context.Users
                .Where(u => u.Department != null) // Assuming instructors have a dept
                .Select(u => new { u.Id, Name = $"{u.FirstName} {u.LastName} ({u.Department})" })
                .ToListAsync();

            var model = new ManageQualificationsViewModel
            {
                CourseId = course.Id,
                CourseName = $"{course.Code} - {course.Name}",
                QualifiedInstructors = qualifications,
                AllInstructors = new SelectList(allInstructors, "Id", "Name")
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddQualification(int courseId, string instructorId, int priority)
        {
            // Check if already assigned
            bool exists = await _context.InstructorQualifications
                .AnyAsync(iq => iq.CourseId == courseId && iq.InstructorId == instructorId);

            if (!exists)
            {
                var q = new InstructorCourse
                {
                    CourseId = courseId,
                    InstructorId = instructorId,
                    Priority = priority
                };
                _context.Add(q);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageQualifications), new { courseId });
        }

        // 3. OFFERING WIZARD: Manage Assignments for a Batch
        public async Task<IActionResult> ManageOffering(int batchId, string year = "2025", string semester = "I")
        {
            var batch = await _context.Batches
                .Include(b => b.Sections)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null) return NotFound();

            var model = new BatchOfferingViewModel
            {
                BatchId = batch.Id,
                BatchName = batch.Name,
                AcademicYear = year,
                Semester = semester,
                Sections = new List<SectionAssignmentViewModel>()
            };

            // Retrieve existing assignments for each section
            foreach (var section in batch.Sections)
            {
                var assignments = await _context.SemesterAssignments
                    .Include(a => a.Course)
                    .Include(a => a.Instructor)
                    .Include(a => a.Room)
                    .Where(a => a.SectionId == section.Id && a.AcademicYear == year && a.Semester == semester)
                    .ToListAsync();

                var rowViewModels = new List<AssignmentRowViewModel>();
                foreach (var assign in assignments)
                {
                    // Calculate CURRENT dynamic load for this instructor
                    int currentLoad = 0;
                    if (assign.Instructor != null)
                    {
                        currentLoad = await _context.SemesterAssignments
                        .Include(a => a.Course) // Ensure Course is included for ContactHours
                        .Where(a => a.InstructorId == assign.InstructorId && a.AcademicYear == year && a.Semester == semester)
                        .SumAsync(a => a.Course.ContactHours);
                    }

                    rowViewModels.Add(new AssignmentRowViewModel
                    {
                        AssignmentId = assign.Id,
                        CourseName = assign.Course.Name,
                        ContactHours = assign.Course.ContactHours,
                        CourseId = assign.CourseId,
                        InstructorName = assign.Instructor?.FullName ?? "Unassigned",
                        RoomName = assign.Room?.Name ?? "Unassigned",
                        CurrentInstructorLoad = currentLoad,
                        InstructorLimit = assign.Instructor?.AvailableHours,
                        Status = assign.Status
                    });
                }

                model.Sections.Add(new SectionAssignmentViewModel
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    Assignments = rowViewModels
                });
            }

            // Populate Dropdowns for Modal (Courses, Rooms)
            ViewBag.AllCourses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            
            return View(model);
        }

        // 4. AJAX: Add a Course to a Section
        [HttpPost]
        public async Task<IActionResult> AddCourseToSection(int sectionId, int courseId, string year, string semester)
        {
            var assignment = new SemesterCourseAssignment
            {
                SectionId = sectionId,
                CourseId = courseId,
                AcademicYear = year,
                Semester = semester,
                Status = "Draft"
            };
            _context.SemesterAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 5. AJAX: Get Eligible Instructors for a Course
        [HttpGet]
        public async Task<IActionResult> GetInstructorsForCourse(int courseId, string year, string semester)
        {
            // 1. Find Instructors Qualified for this Course
            var qualified = await _context.InstructorQualifications
                .Include(iq => iq.Instructor)
                .Where(iq => iq.CourseId == courseId)
                .ToListAsync();

            // Get the contact hours for the course being assigned
            var courseHours = await _context.Courses
                                    .Where(c => c.Id == courseId)
                                    .Select(c => c.ContactHours)
                                    .FirstOrDefaultAsync();

            var result = new List<object>();

            foreach (var q in qualified)
            {
                // 2. Calculate their load on the fly
                var load = await _context.SemesterAssignments
                    .Include(a => a.Course)
                    .Where(a => a.InstructorId == q.InstructorId && a.AcademicYear == year && a.Semester == semester)
                    .SumAsync(a => a.Course.ContactHours);
                var projectLoad = load + courseHours;

                result.Add(new
                {
                    id = q.InstructorId,
                    name = $"{q.Instructor.FirstName} {q.Instructor.LastName}",
                    dept = q.Instructor.Department,
                    load = load,
                    limit = q.Instructor.AvailableHours,
                    projectLoad = projectLoad,
                    isOverloaded = projectLoad >= q.Instructor.AvailableHours
                });
            }

            return Json(result);
        }

        // 6. AJAX: Assign Instructor
        [HttpPost]
        public async Task<IActionResult> AssignInstructor(int assignmentId, string instructorId)
        {
            var assignment = await _context.SemesterAssignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
                
            if (assignment == null) return NotFound();
            var instructor = await _context.Users.FindAsync(instructorId);

            if (instructor == null)
            {
                return BadRequest("Instructor not found.");
            }

            //Calculate current load excluding the current assignment if it was already assigned
            var currentLoad = await _context.SemesterAssignments
                .Include(a => a.Course)
                .Where(a => a.InstructorId == instructorId &&
                            a.AcademicYear == assignment.AcademicYear &&
                            a.Semester == assignment.Semester &&
                            a.Id != assignmentId) //Exclude current assignment's previous state
                .SumAsync(a => a.Course.ContactHours);

            var projectLoad = currentLoad + assignment.Course.ContactHours;

            if (projectLoad > instructor.AvailableHours)
            {
                return BadRequest($"Instructor load limit exceeded. Project Load: {projectLoad} hrs, (Limit: {instructor.AvailableHours} hrs).");
            }

            assignment.InstructorId = instructorId;
            assignment.Status = "Assigned"; // Update status upon assignment
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

//
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Rendering;
// using Microsoft.EntityFrameworkCore;
// using ScheduleCentral.Data;
// using ScheduleCentral.Models;
// using ScheduleCentral.Models.ViewModels;
// using ScheduleCentral.Services;

// namespace ScheduleCentral.Controllers
// {
//     [Authorize(Roles = "Department,Admin,ProgramOfficer")]
//     public class CourseManagerController : Controller
//     {
//         private readonly ApplicationDbContext _context;
//         private readonly UserManager<ApplicationUser> _userManager;
//         private readonly InstructorService _instructorService;

//         public CourseManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, InstructorService instructorService)
//         {
//             _context = context;
//             _userManager = userManager;
//             _instructorService = instructorService;
//         }

//         // GET: Index (Lists Offerings/Batches available for management)
//         public async Task<IActionResult> Index()
//         {
//             var user = await _userManager.GetUserAsync(User);
//             var query = _context.CourseOfferings.AsQueryable();

//             if (!User.IsInRole("Admin") && !User.IsInRole("ProgramOfficer"))
//             {
//                 query = query.Where(co => co.Department == user.Department);
//             }

//             return View(await query.ToListAsync());
//         }

//         // GET: ManageOffering (The Main Wizard)
//         public async Task<IActionResult> ManageOffering(int offeringId)
//         {
//             var offering = await _context.CourseOfferings
//                 .Include(co => co.Sections)
//                     .ThenInclude(s => s.Course)
//                 .Include(co => co.Sections)
//                     .ThenInclude(s => s.Instructor)
//                 .Include(co => co.Sections)
//                     .ThenInclude(s => s.RoomRequirements)
//                         .ThenInclude(rr => rr.RoomType)
//                 .FirstOrDefaultAsync(o => o.Id == offeringId);

//             if (offering == null) return NotFound();

//             // Security Check
//             var user = await _userManager.GetUserAsync(User);
//             if (!User.IsInRole("Admin") && !User.IsInRole("ProgramOfficer") && offering.Department != user.Department)
//             {
//                 return Forbid();
//             }

//             var model = new BatchOfferingViewModel
//             {
//                 BatchId = offering.Id,
//                 BatchName = $"{offering.Department} - {offering.AcademicYear}",
//                 DepartmentName = offering.Department,
//                 AcademicYear = offering.AcademicYear,
//                 Semester = offering.Semester,
//                 Status = offering.Status.ToString(),
//                 Sections = new List<SectionAssignmentViewModel>()
//             };

//             // Group by SectionCode (assuming sections are logical groupings in the offering)
//             var groupedSections = offering.Sections.GroupBy(s => s.SectionCode);

//             foreach (var group in groupedSections)
//             {
//                 var sectionVM = new SectionAssignmentViewModel
//                 {
//                     SectionName = group.Key, // e.g., "Section A"
//                     Assignments = new List<AssignmentRowViewModel>()
//                 };

//                 foreach (var assign in group)
//                 {
//                     // Calculate Load for display
//                     int currentLoad = 0;
//                     int maxLoad = 0;
//                     bool isOverloaded = false;

//                     if (assign.Instructor != null)
//                     {
//                         var stats = (await _instructorService.GetAvailableInstructorsForCourseAsync(assign.CourseId, offering.Semester, offering.AcademicYear))
//                                     .FirstOrDefault(i => i.Id == assign.InstructorId);
//                         if (stats != null)
//                         {
//                             currentLoad = stats.CurrentLoad;
//                             maxLoad = stats.AvailableHours;
//                             isOverloaded = stats.IsOverloaded;
//                         }
//                     }

//                     sectionVM.Assignments.Add(new AssignmentRowViewModel
//                     {
//                         AssignmentId = assign.Id,
//                         CourseId = assign.CourseId,
//                         CourseName = assign.Course.Code + " - " + assign.Course.Name,
//                         ContactHours = assign.Course.ContactHours,
//                         InstructorId = assign.InstructorId,
//                         InstructorName = assign.Instructor?.FullName ?? "Unassigned",
//                         CurrentLoad = currentLoad,
//                         MaxLoad = maxLoad,
//                         IsOverloaded = isOverloaded,
//                         Status = assign.Status,
//                         RoomRequirementsDisplay = assign.RoomRequirements
//                             .Select(rr => $"{rr.RoomType.Name} ({rr.HoursPerWeek}h)")
//                             .ToList()
//                     });
//                 }
//                 model.Sections.Add(sectionVM);
//             }

//             // Load Data for Modals
//             ViewBag.AllCourses = new SelectList(await _context.Courses.Where(c => c.Department == offering.Department).ToListAsync(), "Id", "Name");
//             ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "Id", "Name");

//             return View(model);
//         }

//         // AJAX: Add Course to Section
//         [HttpPost]
//         public async Task<IActionResult> AddCourseToSection(int offeringId, string sectionName, int courseId)
//         {
//             var course = await _context.Courses.FindAsync(courseId);
//             if (course == null) return NotFound("Course not found");

//             var newSection = new CourseOfferingSection
//             {
//                 CourseOfferingId = offeringId,
//                 CourseId = courseId,
//                 SectionCode = sectionName,
//                 AssignedContactHours = course.ContactHours,
//                 Status = "Draft"
//             };

//             _context.CourseOfferingSections.Add(newSection);
//             await _context.SaveChangesAsync();
//             return Ok();
//         }

//         // AJAX: Get Instructors
//         [HttpGet]
//         public async Task<IActionResult> GetInstructorsForCourse(int courseId, int offeringId)
//         {
//             var offering = await _context.CourseOfferings.FindAsync(offeringId);
//             if (offering == null) return NotFound();

//             var instructors = await _instructorService.GetAvailableInstructorsForCourseAsync(courseId, offering.Semester, offering.AcademicYear);
//             return Json(instructors);
//         }

//         // AJAX: Assign Instructor
//         [HttpPost]
//         public async Task<IActionResult> AssignInstructor(int assignmentId, string instructorId)
//         {
//             var assignment = await _context.CourseOfferingSections
//                 .Include(s => s.CourseOffering)
//                 .Include(s => s.Course)
//                 .FirstOrDefaultAsync(s => s.Id == assignmentId);

//             if (assignment == null) return NotFound();

//             // Load Check
//             bool available = await _instructorService.CheckAvailability(instructorId, assignment.Course.ContactHours, assignment.CourseOffering.Semester, assignment.CourseOffering.AcademicYear);
            
//             if (!available && !User.IsInRole("Admin"))
//             {
//                 return BadRequest("Instructor load limit exceeded.");
//             }

//             assignment.InstructorId = instructorId;
//             assignment.Status = "Assigned";
//             await _context.SaveChangesAsync();
//             return Ok();
//         }

//         // AJAX: Save Room Requirements
//         [HttpPost]
//         public async Task<IActionResult> SaveRoomRequirements(int assignmentId, [FromBody] List<RoomRequirementDto> requirements)
//         {
//             var assignment = await _context.CourseOfferingSections
//                 .Include(s => s.RoomRequirements)
//                 .FirstOrDefaultAsync(s => s.Id == assignmentId);

//             if (assignment == null) return NotFound();

//             // Remove existing
//             _context.SectionRoomRequirements.RemoveRange(assignment.RoomRequirements);

//             // Add new
//             foreach (var req in requirements)
//             {
//                 assignment.RoomRequirements.Add(new SectionRoomRequirement
//                 {
//                     RoomTypeId = req.RoomTypeId,
//                     HoursPerWeek = req.Hours
//                 });
//             }

//             await _context.SaveChangesAsync();
//             return Ok();
//         }

//         // --- QUALIFICATION MANAGEMENT ---

//         public async Task<IActionResult> ManageQualifications(int courseId)
//         {
//             var course = await _context.Courses.FindAsync(courseId);
//             if (course == null) return NotFound();

//             var model = new ManageQualificationsViewModel
//             {
//                 CourseId = course.Id,
//                 CourseName = course.Name,
//                 QualifiedInstructors = await _context.InstructorQualifications
//                     .Where(q => q.CourseId == courseId)
//                     .Select(q => new QualifiedInstructorViewModel
//                     {
//                         Id = q.Id,
//                         InstructorId = q.InstructorId,
//                         InstructorName = q.Instructor.FullName,
//                         Department = q.Instructor.Department,
//                         Priority = q.Priority
//                     }).ToListAsync()
//             };

//             var instructors = await _context.Users
//                 .Where(u => u.Department != null)
//                 .ToListAsync(); // In prod filter by role

//             model.AllInstructors = new SelectList(instructors.Select(u => new { Id = u.Id, Name = $"{u.FullName} ({u.Department})" }), "Id", "Name");

//             return View(model);
//         }

//         [HttpPost]
//         public async Task<IActionResult> AddQualification(int courseId, string instructorId, int priority)
//         {
//             if (!_context.InstructorQualifications.Any(q => q.CourseId == courseId && q.InstructorId == instructorId))
//             {
//                 _context.InstructorQualifications.Add(new InstructorCourse
//                 {
//                     CourseId = courseId,
//                     InstructorId = instructorId,
//                     Priority = priority
//                 });
//                 await _context.SaveChangesAsync();
//             }
//             return RedirectToAction(nameof(ManageQualifications), new { courseId });
//         }
        
//         [HttpPost]
//         public async Task<IActionResult> RemoveQualification(int id)
//         {
//             var qual = await _context.InstructorQualifications.FindAsync(id);
//             if (qual != null)
//             {
//                 int cId = qual.CourseId;
//                 _context.InstructorQualifications.Remove(qual);
//                 await _context.SaveChangesAsync();
//                 return RedirectToAction(nameof(ManageQualifications), new { courseId = cId });
//             }
//             return RedirectToAction("Index");
//         }
//     }
// }
//
