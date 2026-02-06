using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    // Represents a semester (e.g., "2025/26 Semester I")
    public class AcademicPeriod
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Semester Name")]
        public string Name { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = false; // Only one active period allowed
    }
}