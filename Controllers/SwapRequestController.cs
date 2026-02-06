using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Services;
using System.Security.Claims;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Instructor,ProgramOfficer,Admin")]
    public class SwapRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SwapRequestController> _logger;
        private readonly NotificationService _notifications;
        private readonly ScheduleSubscriptionService _subscriptions;

        private sealed class MeetingOverlapRow
        {
            public int DayOfWeek { get; set; }
            public int SlotStart { get; set; }
            public int SlotLength { get; set; }
            public int ScheduleGridId { get; set; }
            public string? InstructorId { get; set; }
            public int? RoomId { get; set; }
        }

        public SwapRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SwapRequestController> logger,
            NotificationService notifications,
            ScheduleSubscriptionService subscriptions)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notifications = notifications;
            _subscriptions = subscriptions;
        }

        private async Task<SelectList> BuildPeerMeetingsSelectListAsync(ScheduleMeeting meeting)
        {
            var excludeInstructorId = meeting.InstructorId;
            var requesterGridId = meeting.ScheduleGridId;

            var query = _context.ScheduleMeetings
                .AsNoTracking()
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Include(m => m.Instructor)
                .Where(m => m.SchedulePublicationId == meeting.SchedulePublicationId
                            && m.Id != meeting.Id
                            && !string.IsNullOrWhiteSpace(m.InstructorId)
                            && m.ScheduleGridId == requesterGridId);

            if (!string.IsNullOrWhiteSpace(excludeInstructorId))
                query = query.Where(m => m.InstructorId != excludeInstructorId);

            var peerMeetings = await query
                .OrderBy(m => m.Instructor!.FirstName ?? "")
                .ThenBy(m => m.Instructor!.LastName ?? "")
                .ThenBy(m => m.DayOfWeek)
                .ThenBy(m => m.SlotStart)
                .ToListAsync();

            return new SelectList(
                peerMeetings.Select(m => new
                {
                    Id = m.Id,
                    Label = $"{m.Instructor?.FullName} | {(m.Course != null ? m.Course.Code : "")}" +
                            $" | {DayLabel(m.DayOfWeek)} Slot {m.SlotStart} ({SlotTimeRange(m.SlotStart)})"
                }),
                "Id",
                "Label");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("ProgramOfficer") || User.IsInRole("Admin"))
            {
                var all = await _context.ScheduleSwapRequests
                    .AsNoTracking()
                    .Include(r => r.ScheduleMeeting)
                        .ThenInclude(m => m.Course)
                    .Include(r => r.PeerScheduleMeeting)
                        .ThenInclude(m => m!.Course)
                    .Include(r => r.RequesterInstructor)
                    .Include(r => r.PeerInstructor)
                    .OrderByDescending(r => r.RequestedAtUtc)
                    .Take(200)
                    .ToListAsync();

                return View("Index", new SwapRequestIndexViewModel { IsInstructor = false, CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier), Requests = all });
            }

            if (User.IsInRole("Instructor"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var submitted = await _context.ScheduleSwapRequests
                    .AsNoTracking()
                    .Include(r => r.ScheduleMeeting)
                        .ThenInclude(m => m.Course)
                    .Include(r => r.PeerScheduleMeeting)
                        .ThenInclude(m => m!.Course)
                    .Include(r => r.PeerInstructor)
                    .Where(r => r.RequesterInstructorId == user.Id)
                    .OrderByDescending(r => r.RequestedAtUtc)
                    .ToListAsync();

                var incoming = await _context.ScheduleSwapRequests
                    .AsNoTracking()
                    .Include(r => r.ScheduleMeeting)
                        .ThenInclude(m => m.Course)
                    .Include(r => r.PeerScheduleMeeting)
                        .ThenInclude(m => m!.Course)
                    .Include(r => r.RequesterInstructor)
                    .Where(r => r.PeerInstructorId == user.Id && r.Status == ScheduleSwapRequestStatus.PendingPeerReview)
                    .OrderByDescending(r => r.RequestedAtUtc)
                    .ToListAsync();

                return View("Index", new SwapRequestIndexViewModel
                {
                    IsInstructor = true,
                    CurrentUserId = user.Id,
                    SubmittedRequests = submitted,
                    IncomingPeerRequests = incoming
                });
            }

            return Forbid();
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Create(int meetingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var meeting = await _context.ScheduleMeetings
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .Include(m => m.SchedulePublication)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null) return NotFound();
            if (meeting.SchedulePublication == null || meeting.SchedulePublication.Status != SchedulePublicationStatus.Approved)
            {
                TempData["Error"] = "Swap requests can only be created from an approved schedule.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(meeting.InstructorId, user.Id, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
                return Forbid();

            var pendingStatuses = new[]
            {
                ScheduleSwapRequestStatus.PendingProgramOfficerReview,
                ScheduleSwapRequestStatus.PendingPeerReview,
                ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval
            };

            var alreadyPending = await _context.ScheduleSwapRequests
                .AsNoTracking()
                .AnyAsync(r => r.ScheduleMeetingId == meeting.Id
                               && r.RequesterInstructorId == user.Id
                               && pendingStatuses.Contains(r.Status));

            if (alreadyPending)
            {
                TempData["Error"] = "A swap request for this meeting is already pending. Check Swap Requests for status.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);

            var vm = new SwapRequestCreateViewModel
            {
                MeetingId = meeting.Id,
                CurrentDayOfWeek = meeting.DayOfWeek,
                CurrentSlotStart = meeting.SlotStart,
                SlotLength = meeting.SlotLength,
                Course = meeting.Course != null ? $"{meeting.Course.Code} - {meeting.Course.Name}" : "",
                Mode = ScheduleSwapRequestMode.MoveToEmptySlot,
                TargetDayOfWeek = meeting.DayOfWeek,
                TargetSlotStart = meeting.SlotStart,
                PeerMeetingId = null
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SwapRequestCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var meeting = await _context.ScheduleMeetings
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                .Include(m => m.SchedulePublication)
                .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

            if (meeting == null) return NotFound();
            if (meeting.SchedulePublication == null || meeting.SchedulePublication.Status != SchedulePublicationStatus.Approved)
            {
                TempData["Error"] = "Swap requests can only be created from an approved schedule.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(meeting.InstructorId, user.Id, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
                return Forbid();

            var pendingStatuses = new[]
            {
                ScheduleSwapRequestStatus.PendingProgramOfficerReview,
                ScheduleSwapRequestStatus.PendingPeerReview,
                ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval
            };

            var alreadyPending = await _context.ScheduleSwapRequests
                .AsNoTracking()
                .AnyAsync(r => r.ScheduleMeetingId == meeting.Id
                               && r.RequesterInstructorId == user.Id
                               && pendingStatuses.Contains(r.Status));

            if (alreadyPending)
            {
                TempData["Error"] = "A swap request for this meeting is already pending. Check Swap Requests for status.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Mode == ScheduleSwapRequestMode.MoveToEmptySlot)
            {
                if (model.TargetDayOfWeek < 1 || model.TargetDayOfWeek > 7)
                {
                    TempData["Error"] = "Invalid target day.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (model.TargetSlotStart < 1 || model.TargetSlotStart > 10)
                {
                    TempData["Error"] = "Invalid target slot.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }
            }
            else
            {
                if (!model.PeerMeetingId.HasValue || model.PeerMeetingId.Value <= 0)
                {
                    TempData["Error"] = "Peer meeting is required.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }
            }

            string? peerInstructorId = null;
            int? peerMeetingId = null;

            if (model.Mode == ScheduleSwapRequestMode.SwapWithInstructor)
            {
                var peerMeeting = await _context.ScheduleMeetings
                    .AsNoTracking()
                    .Include(m => m.ScheduleGrid)
                        .ThenInclude(g => g.Section)
                            .ThenInclude(s => s.Batch)
                                .ThenInclude(b => b.Department)
                    .Include(m => m.SchedulePublication)
                    .FirstOrDefaultAsync(m => m.Id == model.PeerMeetingId!.Value);

                if (peerMeeting == null)
                {
                    TempData["Error"] = "Peer meeting not found.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (peerMeeting.SchedulePublication == null || peerMeeting.SchedulePublication.Status != SchedulePublicationStatus.Approved)
                {
                    TempData["Error"] = "Peer meeting must be in an approved schedule.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (peerMeeting.SchedulePublicationId != meeting.SchedulePublicationId)
                {
                    TempData["Error"] = "Peer meeting must be in the same approved schedule term.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(peerMeeting.InstructorId))
                {
                    TempData["Error"] = "Peer meeting has no instructor.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (peerMeeting.ScheduleGridId != meeting.ScheduleGridId)
                {
                    TempData["Error"] = "Peer meeting must be from the same section.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(meeting.InstructorId)
                    && string.Equals(peerMeeting.InstructorId, meeting.InstructorId, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Cannot swap with your own meeting.";
                    ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                    return View(model);
                }

                peerInstructorId = peerMeeting.InstructorId;
                peerMeetingId = peerMeeting.Id;
            }

            var req = new ScheduleSwapRequest
            {
                ScheduleMeetingId = meeting.Id,
                RequesterInstructorId = user.Id,
                Mode = model.Mode,
                PeerInstructorId = peerInstructorId,
                PeerScheduleMeetingId = peerMeetingId,
                TargetDayOfWeek = model.Mode == ScheduleSwapRequestMode.MoveToEmptySlot ? model.TargetDayOfWeek : null,
                TargetSlotStart = model.Mode == ScheduleSwapRequestMode.MoveToEmptySlot ? model.TargetSlotStart : null,
                RequestedAtUtc = DateTime.UtcNow,
                Status = ScheduleSwapRequestStatus.PendingProgramOfficerReview
            };

            _context.ScheduleSwapRequests.Add(req);
            try
            {
                await _context.SaveChangesAsync();

                await _notifications.CreateForRolesAsync(
                    new[] { "ProgramOfficer", "Admin" },
                    "New swap request",
                    "A swap request was submitted and is pending Program Officer review.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                await _notifications.CreateAsync(
                    req.RequesterInstructorId,
                    "Swap request submitted",
                    "Your swap request is pending Program Officer review.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                TempData["Success"] = "Swap request submitted. Track status under Swap Requests.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create swap request for meeting {MeetingId} by user {UserId}", model.MeetingId, user.Id);
                TempData["Error"] = "Failed to submit swap request.";
                ViewBag.PeerMeetings = await BuildPeerMeetingsSelectListAsync(meeting);
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("Admin") && !string.Equals(req.RequesterInstructorId, user.Id, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (req.Status != ScheduleSwapRequestStatus.PendingProgramOfficerReview
                && req.Status != ScheduleSwapRequestStatus.PendingPeerReview
                && req.Status != ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval)
            {
                TempData["Error"] = "Only pending requests can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            _context.ScheduleSwapRequests.Remove(req);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Swap request cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("Admin") && !string.Equals(req.RequesterInstructorId, user.Id, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (req.Status == ScheduleSwapRequestStatus.PendingProgramOfficerReview
                || req.Status == ScheduleSwapRequestStatus.PendingPeerReview
                || req.Status == ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval)
            {
                TempData["Error"] = "Pending requests cannot be deleted. Cancel it instead.";
                return RedirectToAction(nameof(Index));
            }

            _context.ScheduleSwapRequests.Remove(req);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Swap request deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToPeer(int id)
        {
            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Mode != ScheduleSwapRequestMode.SwapWithInstructor)
            {
                TempData["Error"] = "Only Mode A (swap with instructor) requests can be forwarded.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != ScheduleSwapRequestStatus.PendingProgramOfficerReview)
            {
                TempData["Error"] = "Only requests pending Program Officer review can be forwarded.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(req.PeerInstructorId) || !req.PeerScheduleMeetingId.HasValue)
            {
                TempData["Error"] = "Peer instructor/meeting is missing.";
                return RedirectToAction(nameof(Index));
            }

            req.Status = ScheduleSwapRequestStatus.PendingPeerReview;
            req.InitialReviewedAtUtc = DateTime.UtcNow;
            req.InitialReviewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _context.SaveChangesAsync();

                await _notifications.CreateAsync(
                    req.PeerInstructorId!,
                    "Swap request needs your response",
                    "A swap request was forwarded to you. Please agree or disagree.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                TempData["Success"] = "Request forwarded to the peer instructor.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward swap request {RequestId} to peer", id);
                TempData["Error"] = "Failed to forward request.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PeerAgree(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != ScheduleSwapRequestStatus.PendingPeerReview)
            {
                TempData["Error"] = "This request is not awaiting peer review.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(req.PeerInstructorId, user.Id, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
                return Forbid();

            req.PeerDecision = ScheduleSwapPeerDecision.Agreed;
            req.PeerRespondedAtUtc = DateTime.UtcNow;
            req.Status = ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval;

            try
            {
                await _context.SaveChangesAsync();

                await _notifications.CreateAsync(
                    req.RequesterInstructorId,
                    "Peer agreed to swap request",
                    "Peer instructor agreed. Waiting for Program Officer final approval.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                await _notifications.CreateForRolesAsync(
                    new[] { "ProgramOfficer", "Admin" },
                    "Swap request awaiting final approval",
                    "Peer agreed. Swap request is ready for final approval.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                TempData["Success"] = "You agreed to the request. Waiting for Program Officer final approval.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save peer agreement for swap request {RequestId}", id);
                TempData["Error"] = "Failed to submit your response.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PeerDisagree(int id, string feedback)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != ScheduleSwapRequestStatus.PendingPeerReview)
            {
                TempData["Error"] = "This request is not awaiting peer review.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(req.PeerInstructorId, user.Id, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
                return Forbid();

            feedback = (feedback ?? "").Trim();
            if (string.IsNullOrWhiteSpace(feedback))
            {
                TempData["Error"] = "Feedback is required when disagreeing.";
                return RedirectToAction(nameof(Index));
            }

            req.PeerDecision = ScheduleSwapPeerDecision.Disagreed;
            req.PeerRespondedAtUtc = DateTime.UtcNow;
            req.Status = ScheduleSwapRequestStatus.Rejected;
            req.Feedback = feedback;

            try
            {
                await _context.SaveChangesAsync();

                await _notifications.CreateAsync(
                    req.RequesterInstructorId,
                    "Swap request rejected by peer",
                    $"Peer instructor disagreed. Feedback: {feedback}",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                TempData["Success"] = "You disagreed. The request has been rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save peer disagreement for swap request {RequestId}", id);
                TempData["Error"] = "Failed to submit your response.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var req = await _context.ScheduleSwapRequests
                .Include(r => r.ScheduleMeeting)
                    .ThenInclude(m => m.ScheduleGrid)
                        .ThenInclude(g => g.Section)
                            .ThenInclude(s => s.Batch)
                                .ThenInclude(b => b.Department)
                .Include(r => r.ScheduleMeeting)
                    .ThenInclude(m => m.Course)
                .Include(r => r.ScheduleMeeting)
                    .ThenInclude(m => m.SchedulePublication)
                .Include(r => r.PeerScheduleMeeting)
                    .ThenInclude(m => m!.SchedulePublication)
                .FirstOrDefaultAsync(r => r.Id == id);

            IActionResult Fail(string message)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Index));
            }

            if (req == null)
                return Fail("Swap request not found.");

            if (req.Mode == ScheduleSwapRequestMode.MoveToEmptySlot && req.Status != ScheduleSwapRequestStatus.PendingProgramOfficerReview)
                return Fail("Only requests pending Program Officer review can be approved.");

            if (req.Mode == ScheduleSwapRequestMode.SwapWithInstructor && req.Status != ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval)
                return Fail("Only requests pending final approval can be approved.");

            var meeting = req.ScheduleMeeting;
            if (meeting.SchedulePublication == null || meeting.SchedulePublication.Status != SchedulePublicationStatus.Approved)
                return Fail("Request must originate from an approved schedule.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {

            // Create/replace a draft workspace by cloning the approved publication if needed.
            var termYear = meeting.SchedulePublication.AcademicYear;
            var termSem = meeting.SchedulePublication.Semester;

            var hasSubmitted = await _context.SchedulePublications
                .AsNoTracking()
                .AnyAsync(p => p.AcademicYear == termYear
                               && p.Semester == termSem
                               && p.Status == SchedulePublicationStatus.Submitted);

            if (hasSubmitted)
                return Fail("A schedule workspace is currently submitted for approval for this term. Wait for the decision before applying swap requests.");

            var existingDraft = await _context.SchedulePublications
                .Where(p => p.AcademicYear == termYear
                            && p.Semester == termSem
                            && (p.Status == SchedulePublicationStatus.DraftGenerated || p.Status == SchedulePublicationStatus.Rejected))
                .OrderByDescending(p => p.GeneratedAtUtc)
                .FirstOrDefaultAsync();

            if (existingDraft != null)
            {
                var oldDraftMeetings = await _context.ScheduleMeetings
                    .Where(m => m.SchedulePublicationId == existingDraft.Id)
                    .ToListAsync();

                if (oldDraftMeetings.Count > 0)
                    _context.ScheduleMeetings.RemoveRange(oldDraftMeetings);

                _context.SchedulePublications.Remove(existingDraft);
                await _context.SaveChangesAsync();
            }

            var newDraft = new SchedulePublication
            {
                AcademicYear = termYear,
                Semester = termSem,
                Status = SchedulePublicationStatus.DraftGenerated,
                GeneratedAtUtc = DateTime.UtcNow,
                GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            _context.SchedulePublications.Add(newDraft);
            await _context.SaveChangesAsync();

            // Clone meetings
            var approvedMeetings = await _context.ScheduleMeetings
                .AsNoTracking()
                .Where(m => m.SchedulePublicationId == meeting.SchedulePublicationId)
                .ToListAsync();

            foreach (var m in approvedMeetings)
            {
                _context.ScheduleMeetings.Add(new ScheduleMeeting
                {
                    SchedulePublicationId = newDraft.Id,
                    ScheduleGridId = m.ScheduleGridId,
                    AcademicYear = m.AcademicYear,
                    Semester = m.Semester,
                    CourseId = m.CourseId,
                    InstructorId = m.InstructorId,
                    RoomId = m.RoomId,
                    DayOfWeek = m.DayOfWeek,
                    SlotStart = m.SlotStart,
                    SlotLength = m.SlotLength
                });
            }
            await _context.SaveChangesAsync();

            if (req.Mode == ScheduleSwapRequestMode.MoveToEmptySlot)
            {
                if (!req.TargetDayOfWeek.HasValue || !req.TargetSlotStart.HasValue)
                    return Fail("Target slot is missing.");

                var cloned = await _context.ScheduleMeetings
                    .Include(m => m.SchedulePublication)
                    .Include(m => m.Course)
                    .Include(m => m.ScheduleGrid)
                        .ThenInclude(g => g.Section)
                            .ThenInclude(s => s.Batch)
                                .ThenInclude(b => b.Department)
                    .FirstOrDefaultAsync(m => m.SchedulePublicationId == newDraft.Id
                                              && m.ScheduleGridId == meeting.ScheduleGridId
                                              && m.CourseId == meeting.CourseId
                                              && m.InstructorId == meeting.InstructorId
                                              && m.RoomId == meeting.RoomId
                                              && m.DayOfWeek == meeting.DayOfWeek
                                              && m.SlotStart == meeting.SlotStart
                                              && m.SlotLength == meeting.SlotLength);

                if (cloned == null)
                    return Fail("Failed to locate cloned meeting.");

                var section = cloned.ScheduleGrid.Section;
                var isArchitecture = string.Equals(cloned.Course?.Department, "Architecture", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(section.Batch?.Department?.Name, "Architecture", StringComparison.OrdinalIgnoreCase);

                var length = cloned.SlotLength;
                if (length < 1 || length > 10)
                    return Fail("Invalid meeting length.");

                if (req.TargetSlotStart.Value + length - 1 > 10)
                    return Fail("Meeting does not fit in the selected start slot.");

                if (!isArchitecture && CrossesLunchBreak(req.TargetSlotStart.Value, length))
                    return Fail("Lunch break rule violated.");

                for (var s = req.TargetSlotStart.Value; s < req.TargetSlotStart.Value + length; s++)
                {
                    if (!IsSlotAllowed(section.IsExtension, req.TargetDayOfWeek.Value, s))
                        return Fail("Target time is not allowed for this section type.");
                }

                var publicationId = cloned.SchedulePublicationId!.Value;
                var targetEnd = req.TargetSlotStart.Value + length;

                var otherMeetings = await _context.ScheduleMeetings
                    .AsNoTracking()
                    .Where(m => m.SchedulePublicationId == publicationId
                                && m.DayOfWeek == req.TargetDayOfWeek.Value
                                && m.Id != cloned.Id
                                && m.SlotStart < targetEnd
                                && req.TargetSlotStart.Value < (m.SlotStart + m.SlotLength))
                    .Select(m => new { m.ScheduleGridId, m.InstructorId, m.RoomId })
                    .ToListAsync();

                if (otherMeetings.Any(m => m.ScheduleGridId == cloned.ScheduleGridId))
                    return Fail("Conflict: section already has a class at that time.");

                if (cloned.RoomId.HasValue && otherMeetings.Any(m => m.RoomId.HasValue && m.RoomId.Value == cloned.RoomId.Value))
                    return Fail("Conflict: room is already booked at that time.");

                if (!string.IsNullOrWhiteSpace(cloned.InstructorId)
                    && otherMeetings.Any(m => !string.IsNullOrWhiteSpace(m.InstructorId)
                                              && string.Equals(m.InstructorId, cloned.InstructorId, StringComparison.OrdinalIgnoreCase)))
                    return Fail("Conflict: instructor already has a class at that time.");

                cloned.DayOfWeek = req.TargetDayOfWeek.Value;
                cloned.SlotStart = req.TargetSlotStart.Value;

                meeting.SchedulePublication.Status = SchedulePublicationStatus.Archived;
                newDraft.Status = SchedulePublicationStatus.Approved;
                newDraft.ReviewedAtUtc = DateTime.UtcNow;
                newDraft.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                req.Status = ScheduleSwapRequestStatus.Approved;
                req.FinalReviewedAtUtc = DateTime.UtcNow;
                req.FinalReviewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                req.ReviewedAtUtc = req.FinalReviewedAtUtc;
                req.ReviewerUserId = req.FinalReviewerUserId;
                req.AppliedPublicationId = newDraft.Id;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                await _notifications.CreateAsync(
                    req.RequesterInstructorId,
                    "Swap request approved",
                    "Your swap request was approved and the schedule has been updated.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                if (!string.IsNullOrWhiteSpace(req.PeerInstructorId))
                {
                    await _notifications.CreateAsync(
                        req.PeerInstructorId,
                        "Swap request approved",
                        "A swap request involving you was approved and the schedule has been updated.",
                        Url.Action("Index", "SwapRequest", null, Request.Scheme),
                        "SwapRequest");
                }

                var sectionId = meeting.ScheduleGrid?.SectionId;
                if (sectionId.HasValue)
                {
                    var subject = "Schedule updated";
                    var body = $"<p>The schedule has been updated for Section {sectionId.Value} ({termYear} {termSem}).</p>" +
                               $"<p>A swap request was approved and applied.</p>" +
                               $"<p><a href=\"{Url.Action("Public", "Schedule", new { academicYear = termYear, semester = termSem, sectionId = sectionId.Value }, Request.Scheme)}\">View schedule</a></p>";
                    await _subscriptions.SendUpdateAsync(sectionId.Value, termYear, termSem, subject, body);
                }

                TempData["Success"] = "Request approved. Schedule has been updated.";
                return RedirectToAction(nameof(Index));
            }

            if (!req.PeerScheduleMeetingId.HasValue)
                return Fail("Peer meeting is missing.");

            if (req.PeerDecision != ScheduleSwapPeerDecision.Agreed)
                return Fail("Peer has not agreed.");

            var peer = await _context.ScheduleMeetings
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .FirstOrDefaultAsync(m => m.Id == req.PeerScheduleMeetingId.Value);

            if (peer == null) return Fail("Peer meeting not found.");
            if (peer.SchedulePublicationId != meeting.SchedulePublicationId)
                return Fail("Peer meeting must be in the same approved schedule.");

            if (peer.ScheduleGridId != meeting.ScheduleGridId)
                return Fail("Peer meeting must be from the same section.");

            var clonedA = await _context.ScheduleMeetings
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .FirstOrDefaultAsync(m => m.SchedulePublicationId == newDraft.Id
                                          && m.ScheduleGridId == meeting.ScheduleGridId
                                          && m.CourseId == meeting.CourseId
                                          && m.InstructorId == meeting.InstructorId
                                          && m.RoomId == meeting.RoomId
                                          && m.DayOfWeek == meeting.DayOfWeek
                                          && m.SlotStart == meeting.SlotStart
                                          && m.SlotLength == meeting.SlotLength);

            var clonedB = await _context.ScheduleMeetings
                .Include(m => m.Course)
                .Include(m => m.ScheduleGrid)
                    .ThenInclude(g => g.Section)
                        .ThenInclude(s => s.Batch)
                            .ThenInclude(b => b.Department)
                .FirstOrDefaultAsync(m => m.SchedulePublicationId == newDraft.Id
                                          && m.ScheduleGridId == peer.ScheduleGridId
                                          && m.CourseId == peer.CourseId
                                          && m.InstructorId == peer.InstructorId
                                          && m.RoomId == peer.RoomId
                                          && m.DayOfWeek == peer.DayOfWeek
                                          && m.SlotStart == peer.SlotStart
                                          && m.SlotLength == peer.SlotLength);

            if (clonedA == null || clonedB == null)
                return Fail("Failed to locate cloned meetings for swap.");

            var aSection = clonedA.ScheduleGrid.Section;
            var bSection = clonedB.ScheduleGrid.Section;

            var aIsArchitecture = string.Equals(clonedA.Course?.Department, "Architecture", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(aSection.Batch?.Department?.Name, "Architecture", StringComparison.OrdinalIgnoreCase);

            var bIsArchitecture = string.Equals(clonedB.Course?.Department, "Architecture", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(bSection.Batch?.Department?.Name, "Architecture", StringComparison.OrdinalIgnoreCase);

            var aNewDay = clonedB.DayOfWeek;
            var aNewSlot = clonedB.SlotStart;
            var aNewRoom = clonedA.RoomId;

            var bNewDay = clonedA.DayOfWeek;
            var bNewSlot = clonedA.SlotStart;
            var bNewRoom = clonedB.RoomId;

            if (aNewSlot + clonedA.SlotLength - 1 > 10 || bNewSlot + clonedB.SlotLength - 1 > 10)
                return Fail("Swap would place a meeting outside the slot range.");

            if (!aIsArchitecture && CrossesLunchBreak(aNewSlot, clonedA.SlotLength))
                return Fail("Lunch break rule violated for requester meeting.");

            if (!bIsArchitecture && CrossesLunchBreak(bNewSlot, clonedB.SlotLength))
                return Fail("Lunch break rule violated for peer meeting.");

            for (var s = aNewSlot; s < aNewSlot + clonedA.SlotLength; s++)
            {
                if (!IsSlotAllowed(aSection.IsExtension, aNewDay, s))
                    return Fail("Requester meeting target time is not allowed for this section type.");
            }

            for (var s = bNewSlot; s < bNewSlot + clonedB.SlotLength; s++)
            {
                if (!IsSlotAllowed(bSection.IsExtension, bNewDay, s))
                    return Fail("Peer meeting target time is not allowed for this section type.");
            }

            var pubId = newDraft.Id;
            var allMeetings = await _context.ScheduleMeetings
                .AsNoTracking()
                .Where(m => m.SchedulePublicationId == pubId && m.Id != clonedA.Id && m.Id != clonedB.Id)
                .Select(m => new MeetingOverlapRow
                {
                    DayOfWeek = m.DayOfWeek,
                    SlotStart = m.SlotStart,
                    SlotLength = m.SlotLength,
                    ScheduleGridId = m.ScheduleGridId,
                    InstructorId = m.InstructorId,
                    RoomId = m.RoomId
                })
                .ToListAsync();

            if (HasOverlap(allMeetings, aNewDay, aNewSlot, clonedA.SlotLength, clonedA.ScheduleGridId, clonedA.InstructorId, aNewRoom))
                return Fail("Swap causes a conflict for requester meeting.");

            if (HasOverlap(allMeetings, bNewDay, bNewSlot, clonedB.SlotLength, clonedB.ScheduleGridId, clonedB.InstructorId, bNewRoom))
                return Fail("Swap causes a conflict for peer meeting.");

            clonedA.DayOfWeek = aNewDay;
            clonedA.SlotStart = aNewSlot;

            clonedB.DayOfWeek = bNewDay;
            clonedB.SlotStart = bNewSlot;
            

            meeting.SchedulePublication.Status = SchedulePublicationStatus.Archived;
            newDraft.Status = SchedulePublicationStatus.Approved;
            newDraft.ReviewedAtUtc = DateTime.UtcNow;
            newDraft.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            req.Status = ScheduleSwapRequestStatus.Approved;
            req.FinalReviewedAtUtc = DateTime.UtcNow;
            req.FinalReviewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            req.ReviewedAtUtc = req.FinalReviewedAtUtc;
            req.ReviewerUserId = req.FinalReviewerUserId;
            req.AppliedPublicationId = newDraft.Id;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await _notifications.CreateAsync(
                req.RequesterInstructorId,
                "Swap request approved",
                "Your swap request was approved and the schedule has been updated.",
                Url.Action("Index", "SwapRequest", null, Request.Scheme),
                "SwapRequest");

            if (!string.IsNullOrWhiteSpace(req.PeerInstructorId))
            {
                await _notifications.CreateAsync(
                    req.PeerInstructorId,
                    "Swap request approved",
                    "A swap request involving you was approved and the schedule has been updated.",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");
            }

            var sectionId2 = meeting.ScheduleGrid?.SectionId;
            if (sectionId2.HasValue)
            {
                var subject = "Schedule updated";
                var body = $"<p>The schedule has been updated for Section {sectionId2.Value} ({termYear} {termSem}).</p>" +
                           $"<p>A swap request was approved and applied.</p>" +
                           $"<p><a href=\"{Url.Action("Public", "Schedule", new { academicYear = termYear, semester = termSem, sectionId = sectionId2.Value }, Request.Scheme)}\">View schedule</a></p>";
                await _subscriptions.SendUpdateAsync(sectionId2.Value, termYear, termSem, subject, body);
            }

            TempData["Success"] = "Swap approved. Schedule has been updated.";
            return RedirectToAction(nameof(Index));

            static bool HasOverlap(
                List<MeetingOverlapRow> rows,
                int day,
                int slotStart,
                int len,
                int gridId,
                string? instructorId,
                int? roomId)
            {
                var end = slotStart + len;
                foreach (var m in rows)
                {
                    if (m.DayOfWeek != day) continue;
                    if (m.SlotStart >= end) continue;
                    if (slotStart >= (m.SlotStart + m.SlotLength)) continue;

                    if (m.ScheduleGridId == gridId) return true;
                    if (roomId.HasValue && m.RoomId.HasValue && m.RoomId.Value == roomId.Value) return true;
                    if (!string.IsNullOrWhiteSpace(instructorId)
                        && !string.IsNullOrWhiteSpace(m.InstructorId)
                        && string.Equals(m.InstructorId, instructorId, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to approve swap request {RequestId}", id);
                TempData["Error"] = "Failed to approve swap request.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = "ProgramOfficer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string feedback)
        {
            var req = await _context.ScheduleSwapRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
            {
                TempData["Error"] = "Swap request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != ScheduleSwapRequestStatus.PendingProgramOfficerReview
                && req.Status != ScheduleSwapRequestStatus.PendingFinalProgramOfficerApproval)
            {
                TempData["Error"] = "Only requests pending review can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            feedback = (feedback ?? "").Trim();
            if (string.IsNullOrWhiteSpace(feedback))
            {
                TempData["Error"] = "Feedback is required when rejecting a request.";
                return RedirectToAction(nameof(Index));
            }

            req.Status = ScheduleSwapRequestStatus.Rejected;
            req.ReviewedAtUtc = DateTime.UtcNow;
            req.ReviewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            req.Feedback = feedback;

            try
            {
                await _context.SaveChangesAsync();

                await _notifications.CreateAsync(
                    req.RequesterInstructorId,
                    "Swap request rejected",
                    $"Your swap request was rejected. Feedback: {feedback}",
                    Url.Action("Index", "SwapRequest", null, Request.Scheme),
                    "SwapRequest");

                TempData["Success"] = "Swap request rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject swap request {RequestId}", id);
                TempData["Error"] = "Failed to reject swap request.";
            }

            return RedirectToAction(nameof(Index));
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
                _ => $"Day {dayOfWeek}"
            };
        }

        private static string SlotTimeRange(int slot)
        {
            return slot switch
            {
                1 => "08:30 – 09:20",
                2 => "09:30 – 10:20",
                3 => "10:30 – 11:20",
                4 => "11:30 – 12:20",
                5 => "13:30 – 14:20",
                6 => "14:30 – 15:20",
                7 => "15:30 – 16:20",
                8 => "16:30 – 17:20",
                9 => "18:30 – 19:20",
                10 => "19:30 – 20:20",
                _ => ""
            };
        }

        private static bool CrossesLunchBreak(int slotStart1Based, int length)
        {
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

        private static string GetMeetingDepartmentName(ScheduleMeeting meeting)
        {
            return meeting.ScheduleGrid?.Section?.Batch?.Department?.Name
                   ?? meeting.Course?.Department
                   ?? "";
        }
    }

    public class SwapRequestCreateViewModel
    {
        public int MeetingId { get; set; }
        public string Course { get; set; } = "";
        public int CurrentDayOfWeek { get; set; }
        public int CurrentSlotStart { get; set; }
        public int SlotLength { get; set; }

        public ScheduleSwapRequestMode Mode { get; set; } = ScheduleSwapRequestMode.MoveToEmptySlot;

        public int TargetDayOfWeek { get; set; }
        public int TargetSlotStart { get; set; }

        public int? PeerMeetingId { get; set; }
    }

    public class SwapRequestIndexViewModel
    {
        public bool IsInstructor { get; set; }
        public string? CurrentUserId { get; set; }
        public List<ScheduleSwapRequest> Requests { get; set; } = new();
        public List<ScheduleSwapRequest> SubmittedRequests { get; set; } = new();
        public List<ScheduleSwapRequest> IncomingPeerRequests { get; set; } = new();
    }
}
