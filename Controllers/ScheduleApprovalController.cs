using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using System.Security.Claims;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "TopManagement,Admin")]
    public class ScheduleApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifications;
        private readonly ScheduleSubscriptionService _subscriptions;

        public ScheduleApprovalController(
            ApplicationDbContext context,
            NotificationService notifications,
            ScheduleSubscriptionService subscriptions)
        {
            _context = context;
            _notifications = notifications;
            _subscriptions = subscriptions;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var submitted = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.Status == SchedulePublicationStatus.Submitted)
                .OrderByDescending(p => p.SubmittedAtUtc)
                .ToListAsync();

            var ids = submitted.Select(s => s.Id).ToList();

            var counts = await _context.ScheduleMeetings
                .AsNoTracking()
                .Where(m => m.SchedulePublicationId != null && ids.Contains(m.SchedulePublicationId.Value))
                .GroupBy(m => m.SchedulePublicationId)
                .Select(g => new { PublicationId = g.Key!.Value, Meetings = g.Count() })
                .ToDictionaryAsync(x => x.PublicationId, x => x.Meetings);

            var model = submitted.Select(p => new ScheduleApprovalListItemViewModel
            {
                PublicationId = p.Id,
                AcademicYear = p.AcademicYear,
                Semester = p.Semester,
                SubmittedAtUtc = p.SubmittedAtUtc,
                MeetingsCount = counts.TryGetValue(p.Id, out var c) ? c : 0
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var pub = await _context.SchedulePublications
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pub == null) return NotFound();
            if (pub.Status != SchedulePublicationStatus.Submitted) return BadRequest("Only submitted schedules can be reviewed.");

            var meetings = await _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Where(m => m.SchedulePublicationId == pub.Id)
                .ToListAsync();

            var vm = new ScheduleApprovalReviewViewModel
            {
                PublicationId = pub.Id,
                AcademicYear = pub.AcademicYear,
                Semester = pub.Semester,
                SubmittedAtUtc = pub.SubmittedAtUtc,
                MeetingsCount = meetings.Count,
                SectionsCount = meetings.Select(m => m.ScheduleGrid.SectionId).Distinct().Count(),
                Departments = meetings
                    .Select(m => m.ScheduleGrid.Section.Batch.Department.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList(),
                SectionLinks = meetings
                    .GroupBy(m => m.ScheduleGrid.SectionId)
                    .Select(g => new ScheduleApprovalSectionLink
                    {
                        SectionId = g.Key,
                        DisplayName = g.First().ScheduleGrid.Section.Batch.Name + " - " + g.First().ScheduleGrid.Section.Name,
                        IsExtension = g.First().ScheduleGrid.Section.IsExtension
                    })
                    .OrderBy(x => x.DisplayName)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var pub = await _context.SchedulePublications.FirstOrDefaultAsync(p => p.Id == id);
            if (pub == null) return NotFound();
            if (pub.Status != SchedulePublicationStatus.Submitted) return BadRequest("Only submitted schedules can be approved.");

            var otherApproved = await _context.SchedulePublications
                .Where(p => p.Id != pub.Id
                            && p.AcademicYear == pub.AcademicYear
                            && p.Semester == pub.Semester
                            && p.Status == SchedulePublicationStatus.Approved)
                .ToListAsync();

            foreach (var p in otherApproved)
                p.Status = SchedulePublicationStatus.Archived;

            pub.Status = SchedulePublicationStatus.Approved;
            pub.ReviewedAtUtc = DateTime.UtcNow;
            pub.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pub.Feedback = null;

            await _context.SaveChangesAsync();

            await _notifications.CreateForRolesAsync(
                new[] { "ProgramOfficer", "Admin" },
                $"Schedule approved ({pub.AcademicYear} {pub.Semester})",
                "A submitted schedule has been approved and published.",
                Url.Action("Public", "Schedule", new { academicYear = pub.AcademicYear, semester = pub.Semester }, Request.Scheme),
                "Schedule");

            var sectionIds = await _context.ScheduleMeetings
                .AsNoTracking()
                .Where(m => m.SchedulePublicationId == pub.Id)
                .Select(m => m.ScheduleGridId)
                .Distinct()
                .Join(_context.ScheduleGrids.AsNoTracking(), gridId => gridId, g => g.Id, (gridId, g) => g.SectionId)
                .Distinct()
                .ToListAsync();

            foreach (var sectionId in sectionIds)
            {
                var subject = "Schedule published";
                var body = $"<p>A schedule has been published for {pub.AcademicYear} {pub.Semester}.</p>" +
                           $"<p>Section: {sectionId}</p>" +
                           $"<p><a href=\"{Url.Action("Public", "Schedule", new { academicYear = pub.AcademicYear, semester = pub.Semester, sectionId }, Request.Scheme)}\">View schedule</a></p>";
                await _subscriptions.SendUpdateAsync(sectionId, pub.AcademicYear, pub.Semester, subject, body);
            }

            TempData["Success"] = "Schedule approved and published.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string feedback)
        {
            var pub = await _context.SchedulePublications.FirstOrDefaultAsync(p => p.Id == id);
            if (pub == null) return NotFound();
            if (pub.Status != SchedulePublicationStatus.Submitted) return BadRequest("Only submitted schedules can be rejected.");

            feedback = (feedback ?? "").Trim();
            if (string.IsNullOrWhiteSpace(feedback))
            {
                TempData["Error"] = "Feedback is required when rejecting a schedule.";
                return RedirectToAction(nameof(Review), new { id });
            }

            pub.Status = SchedulePublicationStatus.Rejected;
            pub.ReviewedAtUtc = DateTime.UtcNow;
            pub.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pub.Feedback = feedback;

            await _context.SaveChangesAsync();

            await _notifications.CreateForRolesAsync(
                new[] { "ProgramOfficer", "Admin" },
                $"Schedule rejected ({pub.AcademicYear} {pub.Semester})",
                $"Feedback: {feedback}",
                Url.Action("Index", "Schedule", null, Request.Scheme),
                "Schedule");
            TempData["Success"] = "Schedule rejected. Feedback saved.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class ScheduleApprovalListItemViewModel
    {
        public int PublicationId { get; set; }
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public DateTime? SubmittedAtUtc { get; set; }
        public int MeetingsCount { get; set; }
    }

    public class ScheduleApprovalReviewViewModel
    {
        public int PublicationId { get; set; }
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public DateTime? SubmittedAtUtc { get; set; }
        public int MeetingsCount { get; set; }
        public int SectionsCount { get; set; }
        public List<string> Departments { get; set; } = new();
        public List<ScheduleApprovalSectionLink> SectionLinks { get; set; } = new();
    }

    public class ScheduleApprovalSectionLink
    {
        public int SectionId { get; set; }
        public string DisplayName { get; set; } = "";
        public bool IsExtension { get; set; }
    }
}
