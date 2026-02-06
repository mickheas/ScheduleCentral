using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class YearBatch
    {
        public int Id { get; set; }

        [Required]
        public int YearLevelId { get; set; }
        public YearLevel YearLevel { get; set; }

        // Link to the CourseOffering instance for that department+academic-year (nullable until offering created)
        public int? CourseOfferingId { get; set; }
        public CourseOffering? CourseOffering { get; set; }

        [Required]
        public string AcademicYear { get; set; }     // e.g., "2025/26"
        [Required]
        public string Semester { get; set; }         // "I" / "II" / "Summer"

        [Required]
        public string ProgramType { get; set; } = "Regular"; // WILL NEVER BE USED

        // A friendly name (optional)
        public string Name { get; set; }             // e.g., "1st Year - Sem I - Regular"
    }
}
