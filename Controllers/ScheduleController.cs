using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "ProgramOfficer,TopManagement,Admin")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ScheduleSolverService _solver;
        private readonly ILogger<ScheduleController> _logger;
        private readonly NotificationService _notifications;
        private readonly ScheduleSubscriptionService _subscriptions;

        public ScheduleController(
            ApplicationDbContext context,
            ScheduleSolverService solver,
            ILogger<ScheduleController> logger,
            NotificationService notifications,
            ScheduleSubscriptionService subscriptions)
        {
            _context = context;
            _solver = solver;
            _logger = logger;
            _notifications = notifications;
            _subscriptions = subscriptions;
        }

        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> Index()
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();

            var approvedPub = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear && p.Semester == semester && p.Status == SchedulePublicationStatus.Approved)
                .OrderByDescending(p => p.ReviewedAtUtc)
                .FirstOrDefaultAsync();

            var workspacePub = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.DraftGenerated
                                || p.Status == SchedulePublicationStatus.Rejected
                                || p.Status == SchedulePublicationStatus.Submitted))
                .OrderByDescending(p => p.GeneratedAtUtc)
                .FirstOrDefaultAsync();

            var vm = new ScheduleWorkspaceViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                WorkspacePublicationId = workspacePub?.Id,
                WorkspaceStatus = workspacePub?.Status,
                WorkspaceGeneratedAtUtc = workspacePub?.GeneratedAtUtc,
                WorkspaceFeedback = workspacePub?.Feedback,
                ApprovedPublicationId = approvedPub?.Id,
                ApprovedAtUtc = approvedPub?.ReviewedAtUtc
            };

            if (workspacePub != null)
            {
                var meetingsQuery = _context.ScheduleMeetings
                    .AsNoTracking()
                    .Include(m => m.ScheduleGrid)
                        .ThenInclude(g => g.Section)
                            .ThenInclude(s => s.Batch)
                                .ThenInclude(b => b.Department)
                    .Where(m => m.SchedulePublicationId == workspacePub.Id);

                var meetings = await meetingsQuery.ToListAsync();
                vm.MeetingsCount = meetings.Count;
                vm.SectionsCount = meetings.Select(m => m.ScheduleGrid.SectionId).Distinct().Count();
                vm.InstructorsCount = meetings.Where(m => m.InstructorId != null).Select(m => m.InstructorId!).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                vm.RoomsCount = meetings.Where(m => m.RoomId.HasValue).Select(m => m.RoomId!.Value).Distinct().Count();
                vm.Departments = meetings
                    .Select(m => m.ScheduleGrid.Section.Batch.Department.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();

                vm.SectionLinks = meetings
                    .GroupBy(m => m.ScheduleGrid.SectionId)
                    .Select(g => new ScheduleWorkspaceSectionLink
                    {
                        SectionId = g.Key,
                        DisplayName = g.First().ScheduleGrid.Section.Batch.Name + " - " + g.First().ScheduleGrid.Section.Name,
                        IsExtension = g.First().ScheduleGrid.Section.IsExtension
                    })
                    .OrderBy(x => x.DisplayName)
                    .ToList();
            }

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "ProgramOfficer,TopManagement,Admin")]
        public async Task<IActionResult> MeetingsHistory(
            string? academicYear,
            string? semester,
            int? publicationId,
            string? department,
            string? instructorId,
            int? roomId,
            int page = 1,
            int pageSize = 50)
        {
            var (activeYear, activeSem) = await GetActiveAcademicYearSemesterAsync();
            academicYear = string.IsNullOrWhiteSpace(academicYear) ? activeYear : academicYear.Trim();
            semester = string.IsNullOrWhiteSpace(semester) ? activeSem : semester.Trim();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 200);

            var vm = new ScheduleMeetingsHistoryViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                PublicationId = publicationId,
                Department = department,
                InstructorId = instructorId,
                RoomId = roomId,
                Page = page,
                PageSize = pageSize
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

            IQueryable<SchedulePublication> eligiblePublicationsQuery = _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.Approved || p.Status == SchedulePublicationStatus.Rejected));

            if (publicationId.HasValue)
            {
                eligiblePublicationsQuery = eligiblePublicationsQuery.Where(p => p.Id == publicationId.Value);
            }

            var eligiblePublicationIds = await eligiblePublicationsQuery
                .OrderByDescending(p => p.ReviewedAtUtc)
                .Select(p => p.Id)
                .ToListAsync();

            var meetingsQuery = _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.SchedulePublication)
                .Include(m => m.Course)
                .Include(m => m.Room)
                .Include(m => m.Instructor)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Where(m => m.SchedulePublicationId.HasValue
                            && eligiblePublicationIds.Contains(m.SchedulePublicationId.Value));

            if (!string.IsNullOrWhiteSpace(department))
            {
                var deptTrim = department.Trim();
                meetingsQuery = meetingsQuery.Where(m => m.ScheduleGrid.Section.Batch.Department.Name == deptTrim);
            }

            if (!string.IsNullOrWhiteSpace(instructorId))
            {
                meetingsQuery = meetingsQuery.Where(m => m.InstructorId == instructorId);
            }

            if (roomId.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(m => m.RoomId == roomId.Value);
            }

            vm.TotalCount = await meetingsQuery.CountAsync();

            var meetings = await meetingsQuery
                .OrderBy(m => m.ScheduleGrid.Section.Batch.Department.Name)
                .ThenBy(m => m.ScheduleGrid.Section.Batch.Name)
                .ThenBy(m => m.ScheduleGrid.Section.Name)
                .ThenBy(m => m.DayOfWeek)
                .ThenBy(m => m.SlotStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            vm.Departments = await _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Where(m => m.SchedulePublicationId.HasValue && eligiblePublicationIds.Contains(m.SchedulePublicationId.Value))
                .Select(m => m.ScheduleGrid.Section.Batch.Department.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var instructorsRaw = await _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.Instructor)
                .Where(m => m.SchedulePublicationId.HasValue
                            && eligiblePublicationIds.Contains(m.SchedulePublicationId.Value)
                            && m.InstructorId != null
                            && m.Instructor != null)
                .Select(m => new { Id = m.InstructorId!, m.Instructor!.FirstName, m.Instructor!.LastName, m.Instructor!.Email })
                .Distinct()
                .ToListAsync();

            vm.Instructors = instructorsRaw
                .Select(x => new
                {
                    x.Id,
                    Display = string.IsNullOrWhiteSpace(($"{x.FirstName} {x.LastName}").Trim())
                        ? (x.Email ?? x.Id)
                        : ($"{x.FirstName} {x.LastName}").Trim()
                })
                .OrderBy(x => x.Display)
                .Select(x => (x.Id, x.Display))
                .ToList();

            var roomsRaw = await _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.Room)
                .Where(m => m.SchedulePublicationId.HasValue
                            && eligiblePublicationIds.Contains(m.SchedulePublicationId.Value)
                            && m.RoomId.HasValue
                            && m.Room != null)
                .Select(m => new { Id = m.RoomId!.Value, Name = m.Room!.Name })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            vm.Rooms = roomsRaw
                .Select(x => (x.Id, x.Name))
                .ToList();

            vm.Meetings = meetings.Select(m => new ScheduleMeetingHistoryListItem
            {
                MeetingId = m.Id,
                SectionId = m.ScheduleGrid?.SectionId,
                IsExtension = m.ScheduleGrid?.Section?.IsExtension ?? false,
                AcademicYear = m.AcademicYear,
                Semester = m.Semester,
                PublicationId = m.SchedulePublicationId,
                Department = m.ScheduleGrid?.Section?.Batch?.Department?.Name ?? "",
                Batch = m.ScheduleGrid?.Section?.Batch?.Name ?? "",
                Section = m.ScheduleGrid?.Section?.Name ?? "",
                Course = m.Course?.Name ?? "",
                Instructor = m.Instructor?.FullName ?? "",
                Room = m.Room?.Name ?? "",
                DayOfWeek = m.DayOfWeek,
                DayLabel = DayLabel(m.DayOfWeek),
                SlotStart = m.SlotStart,
                SlotLength = m.SlotLength,
                TimeRange = SlotRangeLabel(m.SlotStart, m.SlotLength)
            }).ToList();

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "TopManagement,Admin")]
        public async Task<IActionResult> Audit(
            string? academicYear,
            string? semester,
            int? publicationId,
            string? changedByUserId,
            string? changeType,
            int page = 1,
            int pageSize = 50)
        {
            var (activeYear, activeSem) = await GetActiveAcademicYearSemesterAsync();
            academicYear = string.IsNullOrWhiteSpace(academicYear) ? activeYear : academicYear.Trim();
            semester = string.IsNullOrWhiteSpace(semester) ? activeSem : semester.Trim();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 200);

            var vm = new ScheduleAuditViewModel
            {
                AcademicYear = academicYear,
                Semester = semester,
                PublicationId = publicationId,
                ChangedByUserId = changedByUserId,
                ChangeType = changeType,
                Page = page,
                PageSize = pageSize
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

            var publications = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.Approved || p.Status == SchedulePublicationStatus.Rejected))
                .OrderByDescending(p => p.ReviewedAtUtc)
                .Select(p => new { p.Id, p.Status, p.ReviewedAtUtc })
                .ToListAsync();

            vm.Publications = publications
                .Select(p => (p.Id, $"#{p.Id} - {p.Status}{(p.ReviewedAtUtc.HasValue ? $" ({p.ReviewedAtUtc.Value.ToLocalTime():yyyy-MM-dd})" : "")}"))
                .ToList();

            var logsBase = from l in _context.ScheduleChangeLogs.AsNoTracking()
                           join p in _context.SchedulePublications.AsNoTracking() on l.PublicationId equals p.Id
                           where p.AcademicYear == academicYear
                                 && p.Semester == semester
                                 && (p.Status == SchedulePublicationStatus.Approved || p.Status == SchedulePublicationStatus.Rejected)
                           select l;

            if (publicationId.HasValue)
            {
                logsBase = logsBase.Where(l => l.PublicationId == publicationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(changedByUserId))
            {
                logsBase = logsBase.Where(l => l.ChangedByUserId == changedByUserId);
            }

            if (!string.IsNullOrWhiteSpace(changeType))
            {
                logsBase = logsBase.Where(l => l.ChangeType == changeType);
            }

            vm.ChangeTypes = await _context.ScheduleChangeLogs
                .AsNoTracking()
                .Where(l => publications.Select(p => p.Id).Contains(l.PublicationId) && l.ChangeType != null)
                .Select(l => l.ChangeType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var usersRaw = await _context.ScheduleChangeLogs
                .AsNoTracking()
                .Where(l => publications.Select(p => p.Id).Contains(l.PublicationId) && l.ChangedByUserId != null)
                .Select(l => l.ChangedByUserId!)
                .Distinct()
                .Join(_context.Users.AsNoTracking(), id => id, u => u.Id, (id, u) => new { Id = u.Id, u.FirstName, u.LastName, u.Email })
                .ToListAsync();

            vm.Users = usersRaw
                .Select(x => new
                {
                    x.Id,
                    Display = string.IsNullOrWhiteSpace(($"{x.FirstName} {x.LastName}").Trim())
                        ? (x.Email ?? x.Id)
                        : ($"{x.FirstName} {x.LastName}").Trim()
                })
                .OrderBy(x => x.Display)
                .Select(x => (x.Id, x.Display))
                .ToList();

            vm.TotalCount = await logsBase.CountAsync();

            var logsPage = await logsBase
                .OrderByDescending(l => l.ChangedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var logIds = logsPage.Select(l => l.Id).ToList();

            var logsEnrichedQuery = from l in _context.ScheduleChangeLogs.AsNoTracking()
                                    where logIds.Contains(l.Id)
                                    join user in _context.Users.AsNoTracking() on l.ChangedByUserId equals user.Id into userJoin
                                    from user in userJoin.DefaultIfEmpty()
                                    join meeting in _context.ScheduleMeetings.AsNoTracking() on l.ScheduleMeetingId equals meeting.Id into meetingJoin
                                    from meeting in meetingJoin.DefaultIfEmpty()
                                    join section in _context.Sections.AsNoTracking() on l.SectionId equals section.Id into sectionJoin
                                    from section in sectionJoin.DefaultIfEmpty()
                                    join batch in _context.Batches.AsNoTracking() on section.BatchId equals batch.Id into batchJoin
                                    from batch in batchJoin.DefaultIfEmpty()
                                    join dept in _context.Departments.AsNoTracking() on batch.DepartmentId equals dept.Id into deptJoin
                                    from dept in deptJoin.DefaultIfEmpty()
                                    join course in _context.Courses.AsNoTracking() on l.CourseId equals course.Id into courseJoin
                                    from course in courseJoin.DefaultIfEmpty()
                                    join room in _context.Rooms.AsNoTracking() on l.RoomId equals room.Id into roomJoin
                                    from room in roomJoin.DefaultIfEmpty()
                                    join inst in _context.Users.AsNoTracking() on l.InstructorId equals inst.Id into instJoin
                                    from inst in instJoin.DefaultIfEmpty()
                                    select new
                                    {
                                        Log = l,
                                        ChangedBy = user,
                                        Meeting = meeting,
                                        Department = dept,
                                        Section = section,
                                        Course = course,
                                        Room = room,
                                        Instructor = inst
                                    };

            var logsEnriched = await logsEnrichedQuery.ToListAsync();

            vm.Logs = logsEnriched
                .Select(x =>
                {
                    var slotLen = x.Meeting?.SlotLength ?? 1;
                    return new ScheduleAuditLogItem
                    {
                        ChangedAtUtc = x.Log.ChangedAtUtc,
                        ChangedAtLocal = x.Log.ChangedAtUtc.ToLocalTime().ToString("g"),
                        ChangedByUserId = x.Log.ChangedByUserId,
                        ChangedBy = x.ChangedBy?.FullName ?? x.Log.ChangedByUserId ?? "",
                        PublicationId = x.Log.PublicationId,
                        ScheduleMeetingId = x.Log.ScheduleMeetingId,
                        SectionId = x.Log.SectionId,
                        ChangeType = x.Log.ChangeType ?? "",
                        OldDayOfWeek = x.Log.OldDayOfWeek,
                        OldSlotStart = x.Log.OldSlotStart,
                        NewDayOfWeek = x.Log.NewDayOfWeek,
                        NewSlotStart = x.Log.NewSlotStart,
                        OldLabel = $"{DayLabel(x.Log.OldDayOfWeek)} {SlotRangeLabel(x.Log.OldSlotStart, slotLen)}",
                        NewLabel = $"{DayLabel(x.Log.NewDayOfWeek)} {SlotRangeLabel(x.Log.NewSlotStart, slotLen)}",
                        Department = x.Department?.Name,
                        Section = x.Section != null ? x.Section.Name : null,
                        Course = x.Course?.Name,
                        Instructor = x.Instructor?.FullName,
                        Room = x.Room?.Name
                    };
                })
                .OrderByDescending(x => x.ChangedAtUtc)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> MoveMeeting(int meetingId, int targetDayOfWeek, int targetSlotStart)
        {
            if (meetingId <= 0)
                return BadRequest(new { success = false, error = "Invalid meeting." });

            if (targetDayOfWeek < 1 || targetDayOfWeek > 7)
                return BadRequest(new { success = false, error = "Invalid day." });

            if (targetSlotStart < 1 || targetSlotStart > 10)
                return BadRequest(new { success = false, error = "Invalid slot." });

            var meeting = await _context.ScheduleMeetings
                .Include(m => m.SchedulePublication)
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
                return NotFound(new { success = false, error = "Meeting not found." });

            if (meeting.SchedulePublicationId == null || meeting.SchedulePublication == null)
                return BadRequest(new { success = false, error = "This meeting is not part of a publication workspace." });

            var pubStatus = meeting.SchedulePublication.Status;
            if (pubStatus != SchedulePublicationStatus.DraftGenerated
                && pubStatus != SchedulePublicationStatus.Rejected
                && pubStatus != SchedulePublicationStatus.Approved)
                return BadRequest(new { success = false, error = "This schedule cannot be edited in its current state." });

            var section = meeting.ScheduleGrid?.Section;
            if (section == null)
                return BadRequest(new { success = false, error = "Section not found for meeting." });

            var isArchitecture = string.Equals(meeting.Course?.Department, "Architecture", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(section.Batch?.Department?.Name, "Architecture", StringComparison.OrdinalIgnoreCase);

            var length = meeting.SlotLength;
            if (length < 1 || length > 10)
                return BadRequest(new { success = false, error = "Invalid meeting length." });

            if (targetSlotStart + length - 1 > 10)
                return BadRequest(new { success = false, error = "Meeting does not fit in the selected start slot." });

            if (!isArchitecture && CrossesLunchBreak(targetSlotStart, length))
                return BadRequest(new { success = false, error = "Lunch break rule: a class cannot span slots 4 and 5." });

            for (var s = targetSlotStart; s < targetSlotStart + length; s++)
            {
                if (!IsSlotAllowed(section.IsExtension, targetDayOfWeek, s))
                    return BadRequest(new { success = false, error = "Target time is not allowed for this section type." });
            }

            var publicationId = meeting.SchedulePublicationId.Value;
            var targetEnd = targetSlotStart + length;

            var otherMeetings = await _context.ScheduleMeetings
                .AsNoTracking()
                .Where(m => m.SchedulePublicationId == publicationId
                            && m.DayOfWeek == targetDayOfWeek
                            && m.Id != meeting.Id
                            && m.SlotStart < targetEnd
                            && targetSlotStart < (m.SlotStart + m.SlotLength))
                .Select(m => new { m.Id, m.ScheduleGridId, m.InstructorId, m.RoomId, m.SlotStart, m.SlotLength })
                .ToListAsync();

            if (otherMeetings.Any(m => m.ScheduleGridId == meeting.ScheduleGridId))
                return BadRequest(new { success = false, error = "Conflict: section already has a class at that time." });

            if (meeting.RoomId.HasValue && otherMeetings.Any(m => m.RoomId.HasValue && m.RoomId.Value == meeting.RoomId.Value))
                return BadRequest(new { success = false, error = "Conflict: room is already booked at that time." });

            if (!string.IsNullOrWhiteSpace(meeting.InstructorId)
                && otherMeetings.Any(m => !string.IsNullOrWhiteSpace(m.InstructorId)
                                          && string.Equals(m.InstructorId, meeting.InstructorId, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { success = false, error = "Conflict: instructor already has a class at that time." });

            if (!isArchitecture)
            {
                var siblingDay = await _context.ScheduleMeetings
                    .AsNoTracking()
                    .Where(m => m.SchedulePublicationId == publicationId
                                && m.Id != meeting.Id
                                && m.ScheduleGridId == meeting.ScheduleGridId
                                && m.CourseId == meeting.CourseId)
                    .Select(m => (int?)m.DayOfWeek)
                    .FirstOrDefaultAsync();

                if (siblingDay.HasValue && siblingDay.Value == targetDayOfWeek)
                    return BadRequest(new { success = false, error = "Conflict: this course is split into multiple sessions and cannot be scheduled on the same day." });
            }

            var oldDayOfWeek = meeting.DayOfWeek;
            var oldSlotStart = meeting.SlotStart;

            meeting.DayOfWeek = targetDayOfWeek;
            meeting.SlotStart = targetSlotStart;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _context.ScheduleChangeLogs.Add(new ScheduleChangeLog
            {
                ChangedAtUtc = DateTime.UtcNow,
                ChangedByUserId = userId,
                PublicationId = meeting.SchedulePublicationId.Value,
                ScheduleMeetingId = meeting.Id,
                ScheduleGridId = meeting.ScheduleGridId,
                SectionId = meeting.ScheduleGrid?.SectionId,
                CourseId = meeting.CourseId,
                RoomId = meeting.RoomId,
                InstructorId = meeting.InstructorId,
                OldDayOfWeek = oldDayOfWeek,
                OldSlotStart = oldSlotStart,
                NewDayOfWeek = targetDayOfWeek,
                NewSlotStart = targetSlotStart,
                ChangeType = "MoveMeeting"
            });

            await _context.SaveChangesAsync();

            if (pubStatus == SchedulePublicationStatus.Approved)
            {
                var sectionId = meeting.ScheduleGrid?.SectionId;
                if (sectionId.HasValue)
                {
                    var subject = "Schedule updated";
                    var body = $"<p>The schedule has been updated for Section {sectionId.Value}.</p>" +
                               $"<p>Change: {oldDayOfWeek}:{oldSlotStart} → {targetDayOfWeek}:{targetSlotStart}</p>";
                    await _subscriptions.SendUpdateAsync(sectionId.Value, meeting.AcademicYear, meeting.Semester, subject, body);
                }

                await _notifications.CreateForRolesAsync(
                    new[] { "TopManagement", "Admin" },
                    "Approved schedule edited",
                    "An approved schedule was manually updated.",
                    Url.Action("Public", "Schedule", new { academicYear = meeting.AcademicYear, semester = meeting.Semester, sectionId = meeting.ScheduleGrid?.SectionId }, Request.Scheme),
                    "Schedule");
            }
            return Ok(new { success = true });
        }

        private static bool CrossesLunchBreak(int slotStart1Based, int length)
        {
            // Lunch break is between slots 4 and 5: disallow any meeting that includes both.
            var endSlot = slotStart1Based + length - 1;
            return slotStart1Based <= 4 && endSlot >= 5;
        }

        private static bool IsSlotAllowed(bool isExtension, int dayOfWeek, int slotNumber)
        {
            if (!isExtension)
                return dayOfWeek >= 1 && dayOfWeek <= 5 && slotNumber >= 1 && slotNumber <= 8;

            if (dayOfWeek >= 1 && dayOfWeek <= 5)
                return slotNumber >= 9 && slotNumber <= 10;

            if (dayOfWeek >= 6 && dayOfWeek <= 7)
                return slotNumber >= 1 && slotNumber <= 8;

            return false;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> Generate()
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();

            var hasApproved = await _context.SchedulePublications
                .AsNoTracking()
                .AnyAsync(p => p.AcademicYear == academicYear
                               && p.Semester == semester
                               && p.Status == SchedulePublicationStatus.Approved);

            if (hasApproved)
            {
                TempData["Error"] = "An approved schedule already exists.";
                return RedirectToAction(nameof(Index));
            }

            var hasSubmitted = await _context.SchedulePublications
                .AsNoTracking()
                .AnyAsync(p => p.AcademicYear == academicYear && p.Semester == semester && p.Status == SchedulePublicationStatus.Submitted);

            if (hasSubmitted)
            {
                TempData["Error"] = "A schedule has already been submitted to TopManagement for this term. Wait for approval/rejection before generating a new draft.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _solver.GenerateAndSaveForActiveTermAsync(userId);
            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "Schedule generation failed.";
                return RedirectToAction(nameof(Index));
            }

            await _notifications.CreateForRolesAsync(
                new[] { "ProgramOfficer", "Admin" },
                $"Schedule generated for {academicYear} {semester}",
                $"Meetings: {result.MeetingsCreated}. Assignments: {result.AssignmentsScheduled}/{result.TotalAssignments}.",
                Url.Action("Index", "Schedule", null, Request.Scheme),
                "Schedule");

            TempData["Success"] = $"Schedule generated. Meetings: {result.MeetingsCreated}, Assignments scheduled: {result.AssignmentsScheduled}/{result.TotalAssignments}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> DeleteApproved()
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();

            var approvedIds = await _context.SchedulePublications
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && p.Status == SchedulePublicationStatus.Approved)
                .Select(p => p.Id)
                .ToListAsync();

            if (approvedIds.Count == 0)
            {
                TempData["Success"] = "No approved schedule exists for the active term.";
                return RedirectToAction(nameof(Index));
            }

            var hasSubmitted = await _context.SchedulePublications
                .AsNoTracking()
                .AnyAsync(p => p.AcademicYear == academicYear
                               && p.Semester == semester
                               && p.Status == SchedulePublicationStatus.Submitted);

            if (hasSubmitted)
            {
                TempData["Error"] = "A schedule is currently submitted for approval for this term. Resolve it before deleting the approved schedule.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var meetings = await _context.ScheduleMeetings
                    .Where(m => m.AcademicYear == academicYear
                                && m.Semester == semester
                                && ((m.SchedulePublicationId.HasValue && approvedIds.Contains(m.SchedulePublicationId.Value))
                                    || m.SchedulePublicationId == null))
                    .ToListAsync();

                if (meetings.Count > 0)
                {
                    var meetingIds = meetings.Select(m => m.Id).ToList();

                    var relatedSwapRequests = await _context.ScheduleSwapRequests
                        .Where(r => meetingIds.Contains(r.ScheduleMeetingId)
                                    || (r.PeerScheduleMeetingId.HasValue && meetingIds.Contains(r.PeerScheduleMeetingId.Value)))
                        .ToListAsync();

                    if (relatedSwapRequests.Count > 0)
                        _context.ScheduleSwapRequests.RemoveRange(relatedSwapRequests);
                }

                if (meetings.Count > 0)
                    _context.ScheduleMeetings.RemoveRange(meetings);

                var approvedPublications = await _context.SchedulePublications
                    .Where(p => approvedIds.Contains(p.Id))
                    .ToListAsync();

                if (approvedPublications.Count > 0)
                    _context.SchedulePublications.RemoveRange(approvedPublications);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Approved schedule deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to delete approved schedule for {AcademicYear} {Semester}", academicYear, semester);
                TempData["Error"] = "Failed to delete approved schedule.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> Submit()
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();

            var workspace = await _context.SchedulePublications
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.DraftGenerated || p.Status == SchedulePublicationStatus.Rejected))
                .OrderByDescending(p => p.GeneratedAtUtc)
                .FirstOrDefaultAsync();

            if (workspace == null)
            {
                TempData["Error"] = "No draft schedule exists for the active term. Generate one first.";
                return RedirectToAction(nameof(Index));
            }

            var hasMeetings = await _context.ScheduleMeetings.AsNoTracking().AnyAsync(m => m.SchedulePublicationId == workspace.Id);
            if (!hasMeetings)
            {
                TempData["Error"] = "Draft schedule has no meetings. Generate again before submitting.";
                return RedirectToAction(nameof(Index));
            }

            workspace.Status = SchedulePublicationStatus.Submitted;
            workspace.SubmittedAtUtc = DateTime.UtcNow;
            workspace.SubmittedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            workspace.Feedback = null;

            await _context.SaveChangesAsync();

            await _notifications.CreateForRolesAsync(
                new[] { "TopManagement", "Admin" },
                $"Schedule submitted for approval ({academicYear} {semester})",
                "A draft schedule has been submitted for review.",
                Url.Action("Review", "ScheduleApproval", new { id = workspace.Id }, Request.Scheme),
                "Schedule");
            TempData["Success"] = "Schedule submitted to TopManagement for review.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        public async Task<IActionResult> Clear()
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();

            var workspaceIds = await _context.SchedulePublications
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.DraftGenerated || p.Status == SchedulePublicationStatus.Rejected))
                .Select(p => p.Id)
                .ToListAsync();

            if (workspaceIds.Count == 0)
            {
                TempData["Success"] = "No draft schedule workspace found to clear for the active term.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var meetings = await _context.ScheduleMeetings
                    .Where(m => m.SchedulePublicationId.HasValue && workspaceIds.Contains(m.SchedulePublicationId.Value))
                    .ToListAsync();

                if (meetings.Count == 0)
                {
                    var workspaces = await _context.SchedulePublications
                        .Where(p => workspaceIds.Contains(p.Id))
                        .ToListAsync();

                    if (workspaces.Count > 0)
                        _context.SchedulePublications.RemoveRange(workspaces);

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    TempData["Success"] = "Draft schedule workspace cleared.";
                    return RedirectToAction(nameof(Index));
                }

                var meetingIds = meetings.Select(m => m.Id).ToList();

                var relatedSwapRequests = await _context.ScheduleSwapRequests
                    .Where(r => meetingIds.Contains(r.ScheduleMeetingId)
                                || (r.PeerScheduleMeetingId.HasValue && meetingIds.Contains(r.PeerScheduleMeetingId.Value)))
                    .ToListAsync();

                if (relatedSwapRequests.Count > 0)
                    _context.ScheduleSwapRequests.RemoveRange(relatedSwapRequests);

                _context.ScheduleMeetings.RemoveRange(meetings);

                var workspacesWithMeetings = await _context.SchedulePublications
                    .Where(p => workspaceIds.Contains(p.Id))
                    .ToListAsync();

                if (workspacesWithMeetings.Count > 0)
                    _context.SchedulePublications.RemoveRange(workspacesWithMeetings);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = $"Cleared draft schedule workspace for active term ({academicYear} {semester}). Deleted meetings: {meetings.Count}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to clear draft schedule workspace for {AcademicYear} {Semester}", academicYear, semester);
                TempData["Error"] = "Failed to clear draft schedule workspace.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Public, read-only schedule viewer (no login required)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Public(string? academicYear, string? semester, string? program, int? sectionId, string? programType)
        {
            var model = new PublicScheduleViewModel();

            // Available academic years from approved publications
            model.AcademicYears = await _context.SchedulePublications
                .Where(p => p.Status == SchedulePublicationStatus.Approved)
                .Select(p => p.AcademicYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            var (defaultYear, defaultSemester) = await GetActiveAcademicYearSemesterAsync();

            model.AcademicYear = !string.IsNullOrWhiteSpace(academicYear)
                ? academicYear!
                : (model.AcademicYears.FirstOrDefault() ?? defaultYear);

            model.Semester = !string.IsNullOrWhiteSpace(semester) ? semester! : defaultSemester;
            model.ProgramType = (programType ?? "").Trim();

            var isExtensionFilter = string.Equals(model.ProgramType, "Extension", StringComparison.OrdinalIgnoreCase);
            var isRegularFilter = string.Equals(model.ProgramType, "Regular", StringComparison.OrdinalIgnoreCase);

            var approvedPublicationId = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == model.AcademicYear && p.Semester == model.Semester && p.Status == SchedulePublicationStatus.Approved)
                .OrderByDescending(p => p.ReviewedAtUtc)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (!approvedPublicationId.HasValue)
            {
                model.Programs = new List<string>();
                model.Sections = new List<ScheduleSectionListItem>();
                return View("Public", model);
            }

            // Sections that actually have meetings in this approved schedule
            var sectionIdsWithMeetings = await _context.ScheduleMeetings
                .Where(m => m.SchedulePublicationId == approvedPublicationId.Value)
                .Join(_context.ScheduleGrids,
                    m => m.ScheduleGridId,
                    g => g.Id,
                    (m, g) => g.SectionId)
                .Distinct()
                .ToListAsync();

            var sectionQuery = _context.Sections
                .Include(s => s.Batch)
                    .ThenInclude(b => b.Department)
                .AsQueryable();

            // Programs (departments) that have scheduled sections in this term
            model.Programs = await sectionQuery
                .Where(s => sectionIdsWithMeetings.Contains(s.Id))
                .Select(s => s.Batch.Department.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(program) && model.Programs.Contains(program))
            {
                model.Program = program!;
            }
            else
            {
                model.Program = model.Programs.FirstOrDefault() ?? string.Empty;
            }

            // Sections for the selected program
            var availableSections = await sectionQuery
                .Where(s => sectionIdsWithMeetings.Contains(s.Id)
                            && (string.IsNullOrEmpty(model.Program) || s.Batch.Department.Name == model.Program)
                            && (string.IsNullOrWhiteSpace(model.ProgramType)
                                || (isExtensionFilter && s.IsExtension)
                                || (isRegularFilter && !s.IsExtension)))
                .Select(s => new ScheduleSectionListItem
                {
                    SectionId = s.Id,
                    DisplayName = s.Batch.Name + " - " + s.Name,
                    IsExtension = s.IsExtension
                })
                .OrderBy(s => s.DisplayName)
                .ToListAsync();

            model.Sections = availableSections;

            if (availableSections.Count > 0)
            {
                if (sectionId.HasValue && availableSections.Any(s => s.SectionId == sectionId.Value))
                    model.SectionId = sectionId.Value;
                else
                    model.SectionId = availableSections.First().SectionId;
            }

            // Build timetable and course list for selected section
            if (model.SectionId.HasValue)
            {
                var section = await _context.Sections
                    .Include(s => s.Batch)
                        .ThenInclude(b => b.Department)
                    .FirstOrDefaultAsync(s => s.Id == model.SectionId.Value);

                if (section != null)
                {
                    var days = new List<ScheduleDayViewModel>
                    {
                        new ScheduleDayViewModel { DayOfWeek = 1, Label = "Monday" },
                        new ScheduleDayViewModel { DayOfWeek = 2, Label = "Tuesday" },
                        new ScheduleDayViewModel { DayOfWeek = 3, Label = "Wednesday" },
                        new ScheduleDayViewModel { DayOfWeek = 4, Label = "Thursday" },
                        new ScheduleDayViewModel { DayOfWeek = 5, Label = "Friday" },
                    };
                    if (section.IsExtension)
                    {
                        days.Add(new ScheduleDayViewModel { DayOfWeek = 6, Label = "Saturday" });
                        days.Add(new ScheduleDayViewModel { DayOfWeek = 7, Label = "Sunday" });
                    }

                    var slots = GetSlots(section.IsExtension);

                    var meetings = new List<ScheduleMeeting>();
                    var grid = await _context.ScheduleGrids.FirstOrDefaultAsync(g => g.SectionId == section.Id);
                    if (grid != null)
                    {
                        meetings = await _context.ScheduleMeetings
                            .AsNoTracking()
                            .Include(m => m.Course)
                            .Include(m => m.Instructor)
                            .Include(m => m.Room)
                            .Where(m => m.ScheduleGridId == grid.Id && m.SchedulePublicationId == approvedPublicationId.Value)
                            .ToListAsync();
                    }

                    var cells = BuildCells(section.IsExtension, days, slots, meetings);

                    model.Days = days;
                    model.Slots = slots;
                    model.Cells = cells;

                    var slotLookup = slots.ToDictionary(s => s.SlotNumber, s => s.TimeRange);
                    var dayLookup = days.ToDictionary(d => d.DayOfWeek, d => d.Label);

                    model.Courses = meetings
                        .OrderBy(m => m.DayOfWeek)
                        .ThenBy(m => m.SlotStart)
                        .Select(m => new PublicScheduleCourseListItem
                        {
                            Course = m.Course != null ? $"{m.Course.Code} - {m.Course.Name}" : string.Empty,
                            Time = (dayLookup.TryGetValue(m.DayOfWeek, out var dayLabel) ? dayLabel : $"Day {m.DayOfWeek}")
                                   + " · "
                                   + (slotLookup.TryGetValue(m.SlotStart, out var timeRange) ? timeRange : $"Slot {m.SlotStart}"),
                            Room = m.Room?.Name ?? string.Empty,
                            Instructor = m.Instructor?.FullName ?? string.Empty
                        })
                        .ToList();
                }
            }

            var logQuery = _context.ScheduleChangeLogs
                .AsNoTracking()
                .Where(l => l.PublicationId == approvedPublicationId.Value);

            if (model.SectionId.HasValue)
            {
                var selectedGridId = await _context.ScheduleGrids
                    .AsNoTracking()
                    .Where(g => g.SectionId == model.SectionId.Value)
                    .Select(g => (int?)g.Id)
                    .FirstOrDefaultAsync();

                if (selectedGridId.HasValue)
                    logQuery = logQuery.Where(l => l.ScheduleGridId == selectedGridId.Value);
            }

            model.ChangeLogs = await logQuery
                .OrderByDescending(l => l.ChangedAtUtc)
                .Take(20)
                .Select(l => new PublicScheduleChangeLogItem
                {
                    ChangedAtUtc = l.ChangedAtUtc,
                    Title = l.ChangeType ?? "Schedule updated",
                    Details = $"Moved from Day {l.OldDayOfWeek}, Slot {l.OldSlotStart} to Day {l.NewDayOfWeek}, Slot {l.NewSlotStart}.",
                    ChangedBy = null
                })
                .ToListAsync();

            return View("Public", model);
        }

        [Authorize(Roles = "ProgramOfficer,TopManagement,Admin")]
        public async Task<IActionResult> Grid(int sectionId, int? offeringId = null, int? publicationId = null, string? returnUrl = null)
        {
            var section = await _context.Sections
                .Include(s => s.Batch)
                .ThenInclude(b => b.Department)
                .FirstOrDefaultAsync(s => s.Id == sectionId);
            if (section == null) return NotFound();

            offeringId ??= TryExtractOfferingId(section.Batch?.Name);

            var grid = await _context.Set<ScheduleGrid>().FirstOrDefaultAsync(g => g.SectionId == sectionId);
            if (grid == null)
            {
                grid = new ScheduleGrid { SectionId = sectionId };
                _context.Add(grid);
                await _context.SaveChangesAsync();
            }

            string academicYear;
            string semester;
            SchedulePublication? publication = null;
            if (publicationId.HasValue)
            {
                publication = await _context.SchedulePublications.AsNoTracking().FirstOrDefaultAsync(p => p.Id == publicationId.Value);
                if (publication == null) return NotFound();

                academicYear = publication.AcademicYear;
                semester = publication.Semester;
            }
            else if (offeringId.HasValue)
            {
                var offering = await _context.CourseOfferings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == offeringId.Value);
                if (offering == null) return NotFound();

                academicYear = offering.AcademicYear;
                semester = offering.Semester;
            }
            else
            {
                (academicYear, semester) = await GetActiveAcademicYearSemesterAsync();
            }

            if (!publicationId.HasValue)
            {
                if (offeringId.HasValue)
                {
                    publication = await _context.SchedulePublications
                        .AsNoTracking()
                        .Where(p => p.AcademicYear == academicYear && p.Semester == semester && p.Status == SchedulePublicationStatus.Approved)
                        .OrderByDescending(p => p.ReviewedAtUtc)
                        .FirstOrDefaultAsync();

                    publication ??= await _context.SchedulePublications
                        .AsNoTracking()
                        .Where(p => p.AcademicYear == academicYear
                                    && p.Semester == semester
                                    && (p.Status == SchedulePublicationStatus.DraftGenerated
                                        || p.Status == SchedulePublicationStatus.Rejected
                                        || p.Status == SchedulePublicationStatus.Submitted))
                        .OrderByDescending(p => p.GeneratedAtUtc)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    publication = await _context.SchedulePublications
                        .AsNoTracking()
                        .Where(p => p.AcademicYear == academicYear
                                    && p.Semester == semester
                                    && (p.Status == SchedulePublicationStatus.DraftGenerated
                                        || p.Status == SchedulePublicationStatus.Rejected
                                        || p.Status == SchedulePublicationStatus.Submitted))
                        .OrderByDescending(p => p.GeneratedAtUtc)
                        .FirstOrDefaultAsync();

                    publication ??= await _context.SchedulePublications
                        .AsNoTracking()
                        .Where(p => p.AcademicYear == academicYear && p.Semester == semester && p.Status == SchedulePublicationStatus.Approved)
                        .OrderByDescending(p => p.ReviewedAtUtc)
                        .FirstOrDefaultAsync();
                }
            }

            var meetings = await _context.Set<ScheduleMeeting>()
                .AsNoTracking()
                .Include(m => m.Course)
                .Include(m => m.Instructor)
                .Include(m => m.Room)
                .Where(m => m.ScheduleGridId == grid.Id
                            && m.AcademicYear == academicYear
                            && m.Semester == semester
                            && (publication == null || m.SchedulePublicationId == publication.Id))
                .ToListAsync();

            // Still show unplaced assignments (from SemesterAssignments) for this section+term
            var assignments = await _context.SemesterAssignments
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.Instructor)
                .Include(a => a.Room)
                .Where(a => a.SectionId == sectionId && a.AcademicYear == academicYear && a.Semester == semester)
                .OrderBy(a => a.Course.Code)
                .ToListAsync();

            var days = new List<ScheduleDayViewModel>
            {
                new ScheduleDayViewModel { DayOfWeek = 1, Label = "Monday" },
                new ScheduleDayViewModel { DayOfWeek = 2, Label = "Tuesday" },
                new ScheduleDayViewModel { DayOfWeek = 3, Label = "Wednesday" },
                new ScheduleDayViewModel { DayOfWeek = 4, Label = "Thursday" },
                new ScheduleDayViewModel { DayOfWeek = 5, Label = "Friday" },
            };
            if (section.IsExtension)
            {
                days.Add(new ScheduleDayViewModel { DayOfWeek = 6, Label = "Saturday" });
                days.Add(new ScheduleDayViewModel { DayOfWeek = 7, Label = "Sunday" });
            }

            var slots = GetSlots(section.IsExtension);
            var cells = BuildCells(section.IsExtension, days, slots, meetings);

            var vm = new ScheduleGridViewModel
            {
                OfferingId = offeringId,
                SectionId = section.Id,
                SectionName = section.Name,
                IsExtension = section.IsExtension,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl,
                PublicationId = publication?.Id,
                PublicationStatus = publication?.Status,
                AcademicYear = academicYear,
                Semester = semester,
                Department = section.Batch?.Department.Name,
                Batch = section.Batch?.Name,
                Days = days,
                Slots = slots,
                Cells = cells,
                Assignments = assignments.Select(a => new ScheduleAssignmentListItem
                {
                    AssignmentId = a.Id,
                    Course = a.Course != null ? $"{a.Course.Code} - {a.Course.Name}" : "(Unassigned)",
                    Instructor = a.Instructor != null ? a.Instructor.FullName : "Unassigned",
                    Room = a.Room != null ? a.Room.Name : "Unassigned",
                    Status = a.Status
                }).ToList()
            };

            return View(vm);
        }

        private static int? TryExtractOfferingId(string? batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName)) return null;
            var m = Regex.Match(batchName, @"\bOff\s+(\d+)\b", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            if (int.TryParse(m.Groups[1].Value, out var id)) return id;
            return null;
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

            return (DateTime.Now.Year + "/" + (DateTime.Now.Year + 1), "I");
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

        private static string SlotRangeLabel(int slotStart, int slotLength)
        {
            var start = GetSlotStartTime(slotStart);
            var end = GetSlotEndTime(slotStart + Math.Max(1, slotLength) - 1);
            return $"{start} – {end}";
        }

        private static string GetSlotStartTime(int slot)
        {
            return slot switch
            {
                1 => "08:30",
                2 => "09:30",
                3 => "10:30",
                4 => "11:30",
                5 => "13:30",
                6 => "14:30",
                7 => "15:30",
                8 => "16:30",
                9 => "18:30",
                10 => "19:30",
                _ => "-"
            };
        }

        private static string GetSlotEndTime(int slot)
        {
            return slot switch
            {
                1 => "09:20",
                2 => "10:20",
                3 => "11:20",
                4 => "12:20",
                5 => "14:20",
                6 => "15:20",
                7 => "16:20",
                8 => "17:20",
                9 => "19:20",
                10 => "20:20",
                _ => "-"
            };
        }

        private static List<ScheduleSlotViewModel> GetSlots(bool isExtension)
        {
            var all = new List<ScheduleSlotViewModel>
            {
                new ScheduleSlotViewModel { SlotNumber = 1, TimeRange = "08:30 – 09:20" },
                new ScheduleSlotViewModel { SlotNumber = 2, TimeRange = "09:30 – 10:20" },
                new ScheduleSlotViewModel { SlotNumber = 3, TimeRange = "10:30 – 11:20" },
                new ScheduleSlotViewModel { SlotNumber = 4, TimeRange = "11:30 – 12:20" },
                new ScheduleSlotViewModel { SlotNumber = 5, TimeRange = "13:30 – 14:20" },
                new ScheduleSlotViewModel { SlotNumber = 6, TimeRange = "14:30 – 15:20" },
                new ScheduleSlotViewModel { SlotNumber = 7, TimeRange = "15:30 – 16:20" },
                new ScheduleSlotViewModel { SlotNumber = 8, TimeRange = "16:30 – 17:20" },
                new ScheduleSlotViewModel { SlotNumber = 9, TimeRange = "18:30 – 19:20" },
                new ScheduleSlotViewModel { SlotNumber = 10, TimeRange = "19:30 – 20:20" },
            };

            return isExtension ? all : all.Where(s => s.SlotNumber <= 8).ToList();
        }

        private static Dictionary<string, ScheduleCellViewModel> BuildCells(
            bool isExtension,
            List<ScheduleDayViewModel> days,
            List<ScheduleSlotViewModel> slots,
            List<ScheduleMeeting> meetings)
        {
            var cells = new Dictionary<string, ScheduleCellViewModel>();

            foreach (var day in days)
            {
                foreach (var slot in slots)
                {
                    var disabled = isExtension && ((day.DayOfWeek <= 5 && slot.SlotNumber <= 8) || (day.DayOfWeek >= 6 && slot.SlotNumber >= 9));
                    cells[$"{day.DayOfWeek}:{slot.SlotNumber}"] = new ScheduleCellViewModel { IsDisabled = disabled };
                }
            }

            foreach (var m in meetings)
            {
                for (var s = m.SlotStart; s < m.SlotStart + m.SlotLength; s++)
                {
                    var key = $"{m.DayOfWeek}:{s}";
                    if (!cells.TryGetValue(key, out var cell) || cell.IsDisabled) continue;
                    cell.Course = m.Course?.Name ?? "";
                    cell.Room = m.Room?.Name ?? "";
                    cell.Instructor = m.Instructor?.FullName ?? "";

                    if (s == m.SlotStart)
                    {
                        cell.MeetingId = m.Id;
                        cell.MeetingSlotLength = m.SlotLength;
                        cell.IsMeetingStart = true;
                    }
                }
            }

            return cells;
        }
    }
}
