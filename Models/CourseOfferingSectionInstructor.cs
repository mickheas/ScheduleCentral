using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public class CourseOfferingSectionInstructor
    {
        public int Id { get; set; }

        public int CourseOfferingSectionId { get; set; }
        public CourseOfferingSection CourseOfferingSection { get; set; } = null!;

        public string InstructorId { get; set; } = "";

        [ForeignKey("InstructorId")]
        public ApplicationUser? Instructor { get; set; }
    }
}
