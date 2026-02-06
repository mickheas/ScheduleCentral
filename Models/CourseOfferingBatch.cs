using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class CourseOfferingBatch
    {
        public int Id { get; set; }

        [Required]
        public int CourseOfferingId { get; set; }
        public CourseOffering CourseOffering { get; set; } = null!;

        // Stored as number → Displayed later as 1st/2nd/3rd
        [Range(1, 7)]
        public int YearLevel { get; set; }   // 1, 2, 3, 4...

        //[Required]
        //public string ProgramType { get; set; } = ""; // Regular / Extension

        [Required]
        public string Semester { get; set; } = ""; // I / II

        public bool IsExtension { get; set; } = false;

        [Range(1, 20)]
        public int SectionCount { get; set; } = 1;

       // public CourseOfferingYearLevel YearLevel { get; set; }
        [Required]
        public string BatchName { get; set; } = "Batch I"; // Batch I, Batch II, Batch III
        public string? Notes { get; set; }
        // Navigation to sections under this batch
        public List<CourseOfferingSection> Sections { get; set; } = new List<CourseOfferingSection>();
    }
}
