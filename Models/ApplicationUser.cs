using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    // You can extend the default IdentityUser with custom properties

    public class ApplicationUser : IdentityUser
    {

        [PersonalData]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [PersonalData]
        [StringLength(100)]
        public string? LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsApproved { get; internal set; }

        public DateTime? RegisteredAtUtc { get; set; }

        public bool? IsSelfRegistered { get; set; }

        public string? Department { get; set; } // "Architecture", "Computer Science"
    
        // The maximum hours they can teach (e.g., 12 for Full Time, varying for Part Time)
        [PersonalData]
        [Display(Name = "Available Teaching Hours")]
        public int AvailableHours { get; set; } = 12;

        [PersonalData]
        public string? AvailabilitySlots { get; set; }

        // Navigation property to see all their assignments across the entire system
        public List<CourseOfferingSection> AssignedSections { get; set; } = new List<CourseOfferingSection>();
        
        [NotMapped]
        public int CurrentLoad => AssignedSections?.Sum(s => s.Course?.ContactHours ?? 0) ?? 0;
        // You could also link to a Department table here
        // public int? DepartmentId { get; set; }
        // public virtual Department Department { get; set; }
        public List<InstructorCourse> QualifiedCourses { get; set; } = new List<InstructorCourse>();

}
}
