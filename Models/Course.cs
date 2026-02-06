using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Code")]
        public string Code { get; set; } // e.g., "CS101", "Arch 2422"

        [Required]
        public string Name { get; set; } // e.g., "Basic Design II"

        [Required]
        [Display(Name = "Credit Hours")]
        public int CreditHours { get; set; }

        [Required]
        [Display(Name = "Lecture Hours")]
        public int LectureHours { get; set; }

        [Display(Name = "Lab/Studio Hours")]
        public int LabHours { get; set; }

        public int ContactHours => LectureHours + LabHours;

        // Which department owns this course?
        [Required]
        public string Department { get; set; }
    }
}