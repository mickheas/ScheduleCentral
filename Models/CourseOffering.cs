using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public enum OfferingStatus
    {
        Creation,
        Draft,
        Submitted,
        Approved,
        Rejected
    }

    public class CourseOffering
    {
        public int Id { get; set; }

        [Required]
        public string Department { get; set; } // The department making the offer

        [Required]
        public string AcademicYear { get; set; } // e.g., "2025"

        [Required]
        public string Semester { get; set; } // "I" or "II"

        public OfferingStatus Status { get; set; } = OfferingStatus.Creation;

        public string? RejectionReason { get; set; } // Feedback from PO if rejected

        // Navigation property to the specific rows (classes)
        public List<CourseOfferingSection> Sections { get; set; } = new List<CourseOfferingSection>();
        public List<CourseOfferingBatch> Batches { get; set; } = new ();
        public List<CourseOfferingYearLevel> YearLevels { get; set; } = new();

        /// <summary>
        /// Calculates the completion progress of the offering (0-100%)
        /// Based on 4 stages: Offering Created (25%), Batches Added (25%), Courses Assigned (25%), Instructors & Resources (25%)
        /// </summary>
        public int CalculateProgress()
        {
            int progress = 0;

            // Stage 1: Offering exists (always 25% if we're calling this)
            progress += 25;

            // Stage 2: Has batches
            if (Batches != null && Batches.Any())
                progress += 25;

            // Stage 3: Has sections/courses
            if (Sections != null && Sections.Any())
                progress += 25;

            // Stage 4: Has instructor assignments and room requirements
            bool hasAssignments = Sections != null && Sections.Any(s =>
                !string.IsNullOrEmpty(s.InstructorId) &&
                (s.RoomRequirements != null && s.RoomRequirements.Any()));

            if (hasAssignments)
                progress += 25;

            return progress;
        }
    }
}