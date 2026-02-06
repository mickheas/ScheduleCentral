using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Instructor,Admin")]
    public class InstructorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public InstructorController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> MySchedule()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var active = await _context.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
            if (active == null)
                return View(new InstructorWeeklyScheduleViewModel { InstructorName = user.FullName });

            var academicYear = $"{active.StartDate.Year}/{active.EndDate.Year}";
            var name = (active.Name ?? "").ToLowerInvariant();
            var semester = name.Contains("ii") ? "II" : name.Contains("summer") ? "Summer" : "I";

            var approvedPub = await _context.SchedulePublications
                .AsNoTracking()
                .Where(p => p.AcademicYear == academicYear && p.Semester == semester && p.Status == SchedulePublicationStatus.Approved)
                .OrderByDescending(p => p.ReviewedAtUtc)
                .FirstOrDefaultAsync();

            var vm = new InstructorWeeklyScheduleViewModel
            {
                InstructorId = user.Id,
                InstructorName = user.FullName,
                AcademicYear = academicYear,
                Semester = semester,
                PublicationId = approvedPub?.Id
            };

            if (approvedPub == null)
                return View(vm);

            var meetings = await _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.Course)
                .Include(m => m.Room)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                .Where(m => m.SchedulePublicationId == approvedPub.Id && m.InstructorId == user.Id)
                .ToListAsync();

            var meetingIds = meetings.Select(m => m.Id).ToList();
            var pendingMeetingIds = new HashSet<int>();
            if (meetingIds.Count > 0)
            {
                var pendingStatuses = new[]
                {
                    ScheduleSwapRequestStatus.PendingProgramOfficerReview,
                    ScheduleSwapRequestStatus.PendingPeerReview,
                    ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval
                };

                pendingMeetingIds = (await _context.ScheduleSwapRequests
                        .AsNoTracking()
                        .Where(r => meetingIds.Contains(r.ScheduleMeetingId)
                                    && r.RequesterInstructorId == user.Id
                                    && pendingStatuses.Contains(r.Status))
                        .Select(r => r.ScheduleMeetingId)
                        .Distinct()
                        .ToListAsync())
                    .ToHashSet();
            }

            var meetingItems = meetings
                .OrderBy(m => m.DayOfWeek)
                .ThenBy(m => m.SlotStart)
                .Select(m => new InstructorScheduleMeetingListItem
                {
                    MeetingId = m.Id,
                    Course = m.Course != null ? $"{m.Course.Code} - {m.Course.Name}" : "",
                    Room = m.Room?.Name ?? "",
                    Section = m.ScheduleGrid?.Section?.Batch?.Name + " - " + m.ScheduleGrid?.Section?.Name,
                    DayOfWeek = m.DayOfWeek,
                    SlotStart = m.SlotStart,
                    SlotLength = m.SlotLength
                })
                .ToList();

            foreach (var item in meetingItems)
                item.HasPendingSwapRequest = pendingMeetingIds.Contains(item.MeetingId);

            vm.Meetings = meetingItems;

            var days = new List<ScheduleDayViewModel>
            {
                new ScheduleDayViewModel { DayOfWeek = 1, Label = "Monday" },
                new ScheduleDayViewModel { DayOfWeek = 2, Label = "Tuesday" },
                new ScheduleDayViewModel { DayOfWeek = 3, Label = "Wednesday" },
                new ScheduleDayViewModel { DayOfWeek = 4, Label = "Thursday" },
                new ScheduleDayViewModel { DayOfWeek = 5, Label = "Friday" },
                new ScheduleDayViewModel { DayOfWeek = 6, Label = "Saturday" },
                new ScheduleDayViewModel { DayOfWeek = 7, Label = "Sunday" },
            };

            var slots = new List<ScheduleSlotViewModel>
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

            vm.Days = days;
            vm.Slots = slots;
            vm.Cells = BuildCells(days, slots, meetings);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isLocked = await IsAvailabilityLockedAsync();

            var selected = ParseSlots(user.AvailabilitySlots);

            var vm = new InstructorAvailabilityViewModel
            {
                SelectedSlots = selected.ToList(),
                IsLocked = isLocked
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Availability(InstructorAvailabilityViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await IsAvailabilityLockedAsync())
            {
                TempData["Error"] = "Availability is locked because a schedule has been generated for the active term.";
                return RedirectToAction(nameof(Availability));
            }

            model.SelectedSlots ??= new List<string>();

            var normalized = NormalizeSlots(model.SelectedSlots);
            user.AvailabilitySlots = string.Join(",", normalized);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to save availability.";
                return RedirectToAction(nameof(Availability));
            }

            TempData["Success"] = "Availability saved.";
            return RedirectToAction(nameof(Availability));
        }

        private static HashSet<string> ParseSlots(string? slotsCsv)
        {
            if (string.IsNullOrWhiteSpace(slotsCsv))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return slotsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        public static IEnumerable<string> GetAllowedSlots()
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

        private static Dictionary<string, ScheduleCellViewModel> BuildCells(
            List<ScheduleDayViewModel> days,
            List<ScheduleSlotViewModel> slots,
            List<ScheduleMeeting> meetings)
        {
            var cells = new Dictionary<string, ScheduleCellViewModel>();

            foreach (var day in days)
            {
                foreach (var slot in slots)
                {
                    cells[$"{day.DayOfWeek}:{slot.SlotNumber}"] = new ScheduleCellViewModel { IsDisabled = false };
                }
            }

            foreach (var m in meetings)
            {
                for (var s = m.SlotStart; s < m.SlotStart + m.SlotLength; s++)
                {
                    var key = $"{m.DayOfWeek}:{s}";
                    if (!cells.TryGetValue(key, out var cell)) continue;
                    cell.Course = m.Course != null ? $"{m.Course.Code} - {m.Course.Name}" : "";
                    cell.Room = m.Room?.Name ?? "";
                    cell.Instructor = "";
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

    public class InstructorAvailabilityViewModel
    {
        public List<string> SelectedSlots { get; set; } = new();
        public bool IsLocked { get; set; }
    }
}
