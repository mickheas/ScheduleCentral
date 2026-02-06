using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public class Department
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // e.g., "Architecture"
        public string Code { get; set; } // e.g., "ARCH"
        
        // Navigation
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Batch> Batches { get; set; } = new List<Batch>();
        public string? HeadId { get; set; }
        [ForeignKey("HeadId")]
        public ApplicationUser? Head { get; set; }
    }
}