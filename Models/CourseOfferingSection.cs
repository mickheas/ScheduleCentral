using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ScheduleCentral.Models
{
    public class CourseOfferingSection
    {
        public int Id { get; set; }

        public int? CourseOfferingId { get; set; }
        public CourseOffering? CourseOffering { get; set; }

        public int? OfferingBatchId { get; set; }
        [ForeignKey("OfferingBatchId")]
        public CourseOfferingBatch? OfferingBatch { get; set; }
        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // Specifics for this offering (can override catalog defaults if needed)
        public string YearLevel { get; set; } = ""; // "1st Year", "2nd Year"
        public string SectionName { get; set; } = ""; // "Section A"
        public string ProgramType { get; set; }  = "Regular";// "Regular", "Extension"

        // Instructor Assignment
        public string? InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public ApplicationUser? Instructor { get; set; }

        public List<CourseOfferingSectionInstructor> SectionInstructors { get; set; } = new();

        // Helper to store the load of *this specific section* for history
        public int AssignedContactHours { get; set; }

        public bool IsFullDay { get; set; } = false;

        // Resource Assignment
        public int? RoomId { get; set; }
        public Room? Room { get; set; }

        public List<SectionRoomRequirement> RoomRequirements { get; set; } = new();
        public string? Period { get; set; } // e.g., "Mon 1-4" (Stored as string for now, parsed by algo later)
    }
}