using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class YearLevel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }             // "1st Year", "2nd Year"

        // Department affiliation (string to avoid complex FK issues)
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        // Collections
        public List<YearBatch> Batches { get; set; } = new List<YearBatch>();
    }
}
