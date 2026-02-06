using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public enum ScheduleSwapRequestMode
    {
        MoveToEmptySlot = 0,
        SwapWithInstructor = 1
    }

    public enum ScheduleSwapRequestStatus
    {
        PendingProgramOfficerReview = 0,
        Approved = 1,
        Rejected = 2
        ,
        PendingPeerReview = 3,
        PendingFinalProgramOfficerApproval = 4
    }

    public enum ScheduleSwapPeerDecision
    {
        Pending = 0,
        Agreed = 1,
        Disagreed = 2
    }

    public class ScheduleSwapRequest
    {
        public int Id { get; set; }

        [Required]
        public int ScheduleMeetingId { get; set; }
        public ScheduleMeeting ScheduleMeeting { get; set; } = null!;

        [Required]
        public string RequesterInstructorId { get; set; } = "";
        [ForeignKey(nameof(RequesterInstructorId))]
        public ApplicationUser? RequesterInstructor { get; set; }

        public ScheduleSwapRequestMode Mode { get; set; } = ScheduleSwapRequestMode.MoveToEmptySlot;

        public string? PeerInstructorId { get; set; }
        [ForeignKey(nameof(PeerInstructorId))]
        public ApplicationUser? PeerInstructor { get; set; }

        public int? PeerScheduleMeetingId { get; set; }
        [ForeignKey(nameof(PeerScheduleMeetingId))]
        public ScheduleMeeting? PeerScheduleMeeting { get; set; }

        public int? TargetDayOfWeek { get; set; }

        public int? TargetSlotStart { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

        public ScheduleSwapRequestStatus Status { get; set; } = ScheduleSwapRequestStatus.PendingProgramOfficerReview;

        public string? InitialReviewerUserId { get; set; }
        public DateTime? InitialReviewedAtUtc { get; set; }

        public DateTime? PeerRespondedAtUtc { get; set; }
        public ScheduleSwapPeerDecision PeerDecision { get; set; } = ScheduleSwapPeerDecision.Pending;

        public string? FinalReviewerUserId { get; set; }
        public DateTime? FinalReviewedAtUtc { get; set; }

        public string? ReviewerUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }

        public string? Feedback { get; set; }

        public int? AppliedPublicationId { get; set; }
    }
}
