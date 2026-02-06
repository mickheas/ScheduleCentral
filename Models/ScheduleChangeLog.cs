using System;
using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class ScheduleChangeLog
    {
        public int Id { get; set; }

        [Required]
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

        public string? ChangedByUserId { get; set; }

        [Required]
        public int PublicationId { get; set; }

        [Required]
        public int ScheduleMeetingId { get; set; }

        [Required]
        public int ScheduleGridId { get; set; }

        public int? SectionId { get; set; }

        public int? CourseId { get; set; }

        public int? RoomId { get; set; }

        public string? InstructorId { get; set; }

        [Required]
        public int OldDayOfWeek { get; set; }

        [Required]
        public int OldSlotStart { get; set; }

        [Required]
        public int NewDayOfWeek { get; set; }

        [Required]
        public int NewSlotStart { get; set; }

        [MaxLength(200)]
        public string? ChangeType { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
