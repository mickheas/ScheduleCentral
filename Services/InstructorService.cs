using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;

namespace ScheduleCentral.Services
{
    public class InstructorService
    {
        private const int MaxInstructorLoad = 24;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public InstructorService(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<InstructorLoadViewModel>> GetAvailableInstructorsAsync(string currentSemester, string? academicYear = null)
        {
            // 1. Get ALL instructors from ALL departments (Faculty sharing)
            // We filter by Role "Instructor"
            var instructorRole = await _roleManager.FindByNameAsync("Instructor");

            if (instructorRole == null)
                return new List<InstructorLoadViewModel>();

            var instructors = await _context.Users
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.CourseOffering)
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.Course)
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.OfferingBatch)
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == instructorRole.Id))
                .ToListAsync();


            var result = new List<InstructorLoadViewModel>();

            foreach (var instr in instructors)
            {
                // 2. Calculate Load for the CURRENT semester only
                var currentLoad = (instr.AssignedSections ?? Enumerable.Empty<CourseOfferingSection>())
                    .Where(s => s.CourseOffering != null && string.Equals(s.CourseOffering.Semester, currentSemester, StringComparison.OrdinalIgnoreCase))
                    .Where(s => academicYear == null || (s.CourseOffering != null && string.Equals(s.CourseOffering.AcademicYear, academicYear, StringComparison.OrdinalIgnoreCase)))
                    .Sum(s => (s.AssignedContactHours > 0 ? s.AssignedContactHours : (s.Course?.LectureHours ?? 0) + (s.Course?.LabHours ?? 0))
                              * ((s.OfferingBatch != null && s.OfferingBatch.SectionCount > 0) ? s.OfferingBatch.SectionCount : 1));

                result.Add(new InstructorLoadViewModel
                {
                    Id = instr.Id,
                    FullName = $"{instr.FirstName} {instr.LastName}",
                    Department = instr.Department!,
                    AvailableHours = MaxInstructorLoad,
                    CurrentLoad = currentLoad,
                    IsOverloaded = currentLoad >= MaxInstructorLoad
                });
            }

            return result;
        }
        
        public async Task<bool> CheckAvailability(string instructorId, int newHours, string semester, string? academicYear = null)
        {
            var instructor = await _context.Users
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.CourseOffering)
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.Course)
                .Include(u => u.AssignedSections)
                .ThenInclude(s => s.OfferingBatch)
                .FirstOrDefaultAsync(u => u.Id == instructorId);
                
            if (instructor == null) return false;
            
            var currentLoad = (instructor.AssignedSections ?? Enumerable.Empty<CourseOfferingSection>())
                .Where(s => s.CourseOffering != null && string.Equals(s.CourseOffering.Semester, semester, StringComparison.OrdinalIgnoreCase))
                .Where(s => academicYear == null || (s.CourseOffering != null && string.Equals(s.CourseOffering.AcademicYear, academicYear, StringComparison.OrdinalIgnoreCase)))
                .Sum(s => (s.AssignedContactHours > 0 ? s.AssignedContactHours : (s.Course?.LectureHours ?? 0) + (s.Course?.LabHours ?? 0))
                          * ((s.OfferingBatch != null && s.OfferingBatch.SectionCount > 0) ? s.OfferingBatch.SectionCount : 1));
                    
            return (currentLoad + newHours) <= MaxInstructorLoad;
        }
        public async Task<List<InstructorLoadViewModel>> GetQualifiedInstructorsForCourse(int courseId, string semester, string? academicYear = null)
        {
            // IDs of instructors qualified for this course
            var qualifiedInstructorIds = await _context.InstructorQualifications
                .Where(iq => iq.CourseId == courseId)
                .Select(iq => iq.InstructorId)
                .ToListAsync();

            if (!qualifiedInstructorIds.Any())
                return new List<InstructorLoadViewModel>();

            // All instructors with their current load for this semester
            var allInstructors = await GetAvailableInstructorsAsync(semester, academicYear);

            // Filter down to only those whose Id is in qualifiedInstructorIds
            return allInstructors
                .Where(i => qualifiedInstructorIds.Contains(i.Id))
                .ToList();
        }
    }

    public class InstructorLoadViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public int AvailableHours { get; set; }
        public int CurrentLoad { get; set; }
        public bool IsOverloaded { get; set; }
        
        public string DisplayText => $"{FullName} ({Department}) - Load: {CurrentLoad}/{AvailableHours}";
    }
}