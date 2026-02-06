using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class ScheduleGrid
    {
        public int Id { get; set; }

        [Required]
        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
