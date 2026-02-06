using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    // Junction table: Defines which courses an instructor is qualified/assigned to teach pool
    public class InstructorCourse
    {
        public int Id { get; set; }

        public string InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public ApplicationUser Instructor { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
        
        // Optional: Priority or Preference (e.g., 1 = Primary, 2 = Backup)
        public int Priority { get; set; } = 1;
    }
}