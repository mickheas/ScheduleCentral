using System;
using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class ScheduleEmailSubscription
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = "";

        [Required]
        public int SectionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string AcademicYear { get; set; } = "";

        [Required]
        [MaxLength(20)]
        public string Semester { get; set; } = "";

        [Required]
        [MaxLength(64)]
        public string ConfirmToken { get; set; } = "";

        [Required]
        [MaxLength(64)]
        public string UnsubscribeToken { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ConfirmedAtUtc { get; set; }
    }
}
