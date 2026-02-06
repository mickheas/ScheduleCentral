using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class RoomType
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Type Name")]
        public string Name { get; set; } // e.g., "Lecture Hall", "Design Studio"
        
        public string? Description { get; set; }
    }
}