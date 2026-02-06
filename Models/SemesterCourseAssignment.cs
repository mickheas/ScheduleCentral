using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    // The actual record of a class happening this semester
    public class SemesterCourseAssignment
    {
        public int Id { get; set; }

        // Context
        [Required]
        public string AcademicYear { get; set; } // "2025"
        [Required]
        public string Semester { get; set; } // "I" or "II"

        // Who is taking it?
        public int SectionId { get; set; }
        public Section Section { get; set; }

        // What are they taking?
        public int CourseId { get; set; }
        public Course Course { get; set; }

        // Who is teaching it? (Nullable until assigned)
        public string? InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public ApplicationUser? Instructor { get; set; }

        // Where is it?
        public int? RoomId { get; set; }
        public Room? Room { get; set; }
        
        // Load Calculation Helper
        [NotMapped]
        public int LoadHours => Course?.ContactHours ?? 0;
        //Status
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved
    }
}