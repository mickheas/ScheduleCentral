using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "ProgramOfficer,Department,TopManagement,Instructor,Admin")]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? academicYear, string? semester)
        {
            var (activeYear, activeSemester) = await GetActiveAcademicYearSemesterAsync();
            academicYear = string.IsNullOrWhiteSpace(academicYear) ? activeYear : academicYear.Trim();
            semester = string.IsNullOrWhiteSpace(semester) ? activeSemester : semester.Trim();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;

            var isInstructor = User.IsInRole("Instructor");
            var isDepartment = User.IsInRole("Department");

            var vm = new AnalyticsDashboardViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                PrimaryRole = user != null
                    ? (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User"
                    : "User",
                ScopeDepartment = isDepartment ? (user?.Department ?? "") : null,
                MyAvailableHours = user?.AvailableHours ?? 0
            };

            vm.AcademicYears = await _context.SchedulePublications
                .AsNoTracking()
                .Select(p => p.AcademicYear)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            vm.Semesters = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear)
                .Select(p => p.Semester)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var approvedPubIds = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && p.Status == SchedulePublicationStatus.Approved)
                .Select(p => p.Id)
                .ToListAsync();

            IQueryable<CourseOffering> offeringsQuery = _context.CourseOfferings
                .AsNoTracking()
                .Where(o => o.AcademicYear == academicYear && o.Semester == semester);

            if (isDepartment && !string.IsNullOrWhiteSpace(user?.Department))
            {
                var dept = user.Department.Trim();
                offeringsQuery = offeringsQuery.Where(o => o.Department == dept);
            }

            var offerings = await offeringsQuery.ToListAsync();

            vm.TotalOfferings = offerings.Count;
            vm.ApprovedOfferings = offerings.Count(o => o.Status == OfferingStatus.Approved);
            vm.RejectedOfferings = offerings.Count(o => o.Status == OfferingStatus.Rejected);
            vm.SubmittedOfferings = offerings.Count(o => o.Status == OfferingStatus.Submitted);
            vm.DraftOfferings = offerings.Count(o => o.Status == OfferingStatus.Draft || o.Status == OfferingStatus.Creation);

            var reviewedCount = vm.ApprovedOfferings + vm.RejectedOfferings;
            vm.RejectionRatePercent = reviewedCount == 0 ? 0 : (double)vm.RejectedOfferings / reviewedCount * 100.0;

            IQueryable<ScheduleMeeting> meetingsQuery = _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Where(m => m.SchedulePublicationId.HasValue
                            && approvedPubIds.Contains(m.SchedulePublicationId.Value));

            if (isDepartment && !string.IsNullOrWhiteSpace(user?.Department))
            {
                var dept = user.Department.Trim();
                meetingsQuery = meetingsQuery.Where(m => m.ScheduleGrid.Section.Batch.Department.Name == dept);
            }

            if (isInstructor && userId != null)
            {
                meetingsQuery = meetingsQuery.Where(m => m.InstructorId == userId);
            }

            var meetings = await meetingsQuery.ToListAsync();

            vm.TotalMeetings = meetings.Count;
            vm.RoomsUsed = meetings.Where(m => m.RoomId.HasValue).Select(m => m.RoomId!.Value).Distinct().Count();
            vm.InstructorsUsed = meetings.Where(m => m.InstructorId != null).Select(m => m.InstructorId!).Distinct().Count();

            var occupiedRoomSlots = meetings.Where(m => m.RoomId.HasValue).Sum(m => m.SlotLength);
            var roomCapacitySlots = vm.RoomsUsed * 70;
            vm.RoomUtilizationPercent = roomCapacitySlots == 0 ? 0 : (double)occupiedRoomSlots / roomCapacitySlots * 100.0;

            IQueryable<ScheduleChangeLog> logsBase = from l in _context.ScheduleChangeLogs.AsNoTracking()
                                                    join p in _context.SchedulePublications.AsNoTracking() on l.PublicationId equals p.Id
                                                    where p.AcademicYear == academicYear
                                                          && p.Semester == semester
                                                          && (p.Status == SchedulePublicationStatus.Approved || p.Status == SchedulePublicationStatus.Rejected)
                                                    select l;

            if (isDepartment && !string.IsNullOrWhiteSpace(user?.Department))
            {
                var dept = user.Department.Trim();
                logsBase = from l in logsBase
                           join s in _context.Sections.AsNoTracking() on l.SectionId equals s.Id
                           join b in _context.Batches.AsNoTracking() on s.BatchId equals b.Id
                           join d in _context.Departments.AsNoTracking() on b.DepartmentId equals d.Id
                           where d.Name == dept
                           select l;
            }

            if (isInstructor && userId != null)
            {
                logsBase = logsBase.Where(l => l.InstructorId == userId);
            }

            var logs = await logsBase.ToListAsync();
            vm.ScheduleChanges = logs.Count;
            vm.ChangesPerMeeting = vm.TotalMeetings == 0 ? 0 : (double)vm.ScheduleChanges / vm.TotalMeetings;

            if (isInstructor)
            {
                vm.MyMeetings = vm.TotalMeetings;
                vm.MyScheduledSlots = meetings.Sum(m => m.SlotLength);
                vm.MyLoadUtilizationPercent = vm.MyAvailableHours == 0 ? 0 : (double)vm.MyScheduledSlots / vm.MyAvailableHours * 100.0;
            }

            vm.OfferingsStatusLabelsJson = JsonSerializer.Serialize(new[] { "Approved", "Rejected", "Submitted", "Draft" });
            vm.OfferingsStatusDataJson = JsonSerializer.Serialize(new[] { vm.ApprovedOfferings, vm.RejectedOfferings, vm.SubmittedOfferings, vm.DraftOfferings });

            var roomNames = await _context.Rooms.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name);
            var topRooms = meetings
                .Where(m => m.RoomId.HasValue)
                .GroupBy(m => m.RoomId!.Value)
                .Select(g => new { RoomId = g.Key, Slots = g.Sum(x => x.SlotLength) })
                .OrderByDescending(x => x.Slots)
                .Take(7)
                .ToList();

            vm.TopRoomsLabelsJson = JsonSerializer.Serialize(topRooms.Select(x => roomNames.TryGetValue(x.RoomId, out var name) ? name : $"Room {x.RoomId}").ToList());
            vm.TopRoomsDataJson = JsonSerializer.Serialize(topRooms.Select(x => x.Slots).ToList());

            var changesByDay = logs
                .GroupBy(l => l.ChangedAtUtc.Date)
                .OrderBy(g => g.Key)
                .TakeLast(14)
                .ToList();

            vm.ChangesOverTimeLabelsJson = JsonSerializer.Serialize(changesByDay.Select(g => g.Key.ToString("MM-dd")).ToList());
            vm.ChangesOverTimeDataJson = JsonSerializer.Serialize(changesByDay.Select(g => g.Count()).ToList());

            var meetingsByDay = meetings
                .GroupBy(m => m.DayOfWeek)
                .OrderBy(g => g.Key)
                .ToList();

            vm.MeetingsByDayLabelsJson = JsonSerializer.Serialize(meetingsByDay.Select(g => DayLabel(g.Key)).ToList());
            vm.MeetingsByDayDataJson = JsonSerializer.Serialize(meetingsByDay.Select(g => g.Sum(x => x.SlotLength)).ToList());

            if (!isInstructor)
            {
                var byDept = meetings
                    .GroupBy(m => m.ScheduleGrid.Section.Batch.Department.Name)
                    .OrderByDescending(g => g.Sum(x => x.SlotLength))
                    .Take(7)
                    .ToList();

                vm.MeetingsByDepartmentLabelsJson = JsonSerializer.Serialize(byDept.Select(g => g.Key).ToList());
                vm.MeetingsByDepartmentDataJson = JsonSerializer.Serialize(byDept.Select(g => g.Sum(x => x.SlotLength)).ToList());
            }

            return View(vm);
        }

        private async Task<(string AcademicYear, string Semester)> GetActiveAcademicYearSemesterAsync()
        {
            var active = await _context.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
            if (active != null)
            {
                var academicYear = $"{active.StartDate.Year}/{active.EndDate.Year}";
                var name = (active.Name ?? "").ToLowerInvariant();
                var sem = name.Contains("ii") ? "II" : name.Contains("summer") ? "Summer" : "I";
                return (academicYear, sem);
            }

            return (System.DateTime.Now.Year + "/" + (System.DateTime.Now.Year + 1), "I");
        }

        private static string DayLabel(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => dayOfWeek.ToString()
            };
        }
    }
}
