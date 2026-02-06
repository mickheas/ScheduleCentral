using Microsoft.AspNetCore.Mvc.Rendering;
using ScheduleCentral.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScheduleCentral.Controllers;

namespace ScheduleCentral.Models.ViewModels
{
    // Index page top-level grouping
    public class CourseOfferingIndexViewModel
    {
        // Each YearProgram groups YearLevel + ProgramType (e.g., "1 - Regular")
        public List<YearProgramViewModel> YearPrograms { get; set; } = new List<YearProgramViewModel>();
        public List<CourseOfferingIndexCardViewModel> CourseOfferings { get; set; }

    }

    public class YearProgramViewModel
    {
        public int Id { get; set; }                 // YearLevel Id
        public string YearLevel { get; set; } = ""; // e.g., "1"
        public string ProgramType { get; set; } = "Regular"; // "Regular" / "Extension"
        public List<BatchViewModel> Batches { get; set; } = new List<BatchViewModel>();
    }

    public class BatchViewModel
    {
        public int Id { get; set; }
        public int YearLevel {get; set;}       // YearBatch Id
        public string BatchName { get; set; } = ""; // Friendly name
        public int DisplayOrder { get; set; }
        public string SemesterName { get; set; } = "I";
        public int? CourseOfferingId { get; set; }  // Linked CourseOffering (nullable)
        public OfferingStatus Status { get; set; }
        public bool IsExtension { get; set; }
        public int SectionCount { get; set; } = 1;
        public List<BatchCourseViewModel> Courses { get; set; } = new List<BatchCourseViewModel>();
    }

    public class BatchCourseViewModel
    {
        public int Id { get; set; }                 // CourseOfferingSection.Id
        public int CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public string InstructorName { get; set; } = "Unassigned";
        public List<string> RoomTypes { get; set; } = new List<string>();
        public int ContactHours { get; set; }
        public bool IsFullDay { get; set; }
        public string Status { get; set; } = "Draft";
    }

    // Create (CourseOffering) view model
    public class CourseOfferingCreateViewModel
    {
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "I";
        public OfferingStatus Status { get; set; } = OfferingStatus.Creation;

        // For UI dropdowns (optional)
        public IEnumerable<SelectListItem> YearLevels { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProgramTypes { get; set; } = new List<SelectListItem>();
    }

    public class OfferingFeedbackViewModel
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string RejectionReason { get; set; } = "";
    }

    // Edit main page VM (aggregate)
    public class CourseOfferingEditViewModel
    {
        public int Id { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "I";
        public OfferingStatus Status { get; set; }

        public int? CourseId { get; set; } // Added this property to fix the error
        public int? InstructorId { get; set; } // Ensure this exists if used in the Razor file
        public List<int> SelectedRoomTypeIds { get; set; } = new List<int>(); // Ensure this exists if used in the Razor file
        public int ContactHours { get; set; } // Ensure this exists if used in the Razor file

        // Sections in this offering (editable)
        public List<CourseOfferingSectionEditVM> Sections { get; set; } = new List<CourseOfferingSectionEditVM>();

        // dropdowns
        public IEnumerable<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Instructors { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> RoomTypes { get; set; } = new List<SelectListItem>();
    }

    public class CourseOfferingSectionEditVM
    {
        public int Id { get; set; } // CourseOfferingSection.Id
        public int CourseId { get; set; }
        public string? InstructorId { get; set; }
        public string YearLevel { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string ProgramType { get; set; } = "Regular";
        public int AssignedContactHours { get; set; }

        public List<RoomRequirementVM> RoomRequirements { get; set; } = new List<RoomRequirementVM>();
    }

    public class RoomRequirementVM
    {
        public int Id { get; set; } // SectionRoomRequirement.Id
        public int RoomTypeId { get; set; }
        public int HoursPerWeek { get; set; }
    }

    // Details
    public class CourseOfferingDetailsViewModel
    {
        public int Id { get; set; }
        public string CourseName { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public List<string> RoomTypes { get; set; } = new List<string>();
        public int ContactHours { get; set; }
        public string Status { get; set; } = "";
    }

    // Delete
    public class CourseOfferingDeleteViewModel
    {
        public int Id { get; set; }
        public string CourseName { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string Status { get; set; } = "";
    }

    // DTO for AddCourseToBatch AJAX
    public class AddCourseToBatchDto
    {
        public int BatchId { get; set; }
        public int CourseId { get; set; }
        public string? InstructorId { get; set; }
        public List<int>? RoomTypeIds { get; set; }
        public Dictionary<int, int>? HoursPerRoomType { get; set; } // key: roomTypeId -> hours
    }

        public class ManageBatchViewModel
        {
            public int BatchId { get; set; }
            public string BatchName { get; set; }
            public string YearLevelName { get; set; }
            public string Semester { get; set; }
            public int OfferingId { get; set; }
            public int YearLevelId { get; set; }
            public List<CourseViewModel> Courses { get; set; } = new List<CourseViewModel>();
        }

        public class CourseViewModel
        {
            public int SectionId { get; set; }
            public string CourseName { get; set; }
            public int ContactHours { get; set; }
            public List<RoomTypeViewModel> RoomTypes { get; set; } = new List<RoomTypeViewModel>();
            public string InstructorName { get; set; }
        }

        public class RoomTypeViewModel
        {
            public string TypeName { get; set; }
            public int HoursPerWeek { get; set; }
        }

        public class CreateYearLevelViewModel
        {
        public int Name { get; set; } // Assuming 'Name' is an integer representing the numeric year level
        public int DepartmentId { get; set; }
        public IEnumerable<SelectListItem> Departments { get; set; }
        public int OfferingId { get; set; }

        [Required]
        [Range(1, 6)]
        public int YearLevel { get; set; } // numeric 1,2,3...

        [Required]
        public string ProgramType { get; set; } // Regular / Extension

        [Required]
        public string Semester { get; set; } // I / II
    }
}