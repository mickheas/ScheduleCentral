using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public enum SchedulePublicationStatus
    {
        DraftGenerated = 0,
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Archived = 4
    }

    public class SchedulePublication
    {
        public int Id { get; set; }

        [Required]
        public string AcademicYear { get; set; } = "";

        [Required]
        public string Semester { get; set; } = "";

        public SchedulePublicationStatus Status { get; set; } = SchedulePublicationStatus.DraftGenerated;

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string? GeneratedByUserId { get; set; }

        public DateTime? SubmittedAtUtc { get; set; }
        public string? SubmittedByUserId { get; set; }

        public DateTime? ReviewedAtUtc { get; set; }
        public string? ReviewedByUserId { get; set; }

        public string? Feedback { get; set; }

        public List<ScheduleMeeting> Meetings { get; set; } = new();
    }
}
