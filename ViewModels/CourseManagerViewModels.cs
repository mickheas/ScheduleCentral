using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScheduleCentral.Models.ViewModels
{
    public class ManageQualificationsViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public List<QualifiedInstructorViewModel> QualifiedInstructors { get; set; }
        public SelectList AllInstructors { get; set; }
    }

    public class QualifiedInstructorViewModel
    {
        public int Id { get; set; } // The Qualification ID
        public string InstructorName { get; set; }
        public string Department { get; set; }
        public int Priority { get; set; }
    }

    public class BatchOfferingViewModel
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; }
        public string AcademicYear { get; set; }
        public string Semester { get; set; }
        
        // List of assignments grouped by Section
        public List<SectionAssignmentViewModel> Sections { get; set; }
    }

    public class SectionAssignmentViewModel
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public List<AssignmentRowViewModel> Assignments { get; set; }
    }

    public class AssignmentRowViewModel
    {
        public int AssignmentId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int ContactHours { get; set; }
        public string? InstructorName { get; set; } = "Unassigned";
        public int? CurrentInstructorLoad { get; set; }
        public int? InstructorLimit { get; set; }
        public string? RoomName { get; set; } = "Unassigned";
        public string Status { get; set; } = "Draft";
    }
}