using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    // Represents a "Year" of students (e.g., "Architecture 3rd Year")
    public class Batch
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } // e.g., "3rd Year", "Freshman"
        
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        public List<Section> Sections { get; set; } = new List<Section>();
    }
}