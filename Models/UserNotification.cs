using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public class UserNotification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(2000)]
        public string? Message { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public DateTime? ReadAtUtc { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }
    }
}
