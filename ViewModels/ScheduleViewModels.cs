using ScheduleCentral.Models;

namespace ScheduleCentral.Models.ViewModels
{
    public class ScheduleSectionListItem
    {
        public int SectionId { get; set; }
        public string DisplayName { get; set; } = "";
        public bool IsExtension { get; set; }
    }

    public class ScheduleIndexViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "I";
        public int? SectionId { get; set; }
        public List<ScheduleSectionListItem> Sections { get; set; } = new();
    }

    public class ScheduleWorkspaceViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public int? WorkspacePublicationId { get; set; }
        public SchedulePublicationStatus? WorkspaceStatus { get; set; }
        public DateTime? WorkspaceGeneratedAtUtc { get; set; }
        public string? WorkspaceFeedback { get; set; }

        public int MeetingsCount { get; set; }
        public int SectionsCount { get; set; }
        public int InstructorsCount { get; set; }
        public int RoomsCount { get; set; }
        public List<string> Departments { get; set; } = new();

        public List<ScheduleWorkspaceSectionLink> SectionLinks { get; set; } = new();

        public int? ApprovedPublicationId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
    }

    public class ScheduleWorkspaceSectionLink
    {
        public int SectionId { get; set; }
        public string DisplayName { get; set; } = "";
        public bool IsExtension { get; set; }
    }

    public class ScheduleDayViewModel
    {
        public int DayOfWeek { get; set; } // 1..7
        public string Label { get; set; } = "";
    }

    public class ScheduleSlotViewModel
    {
        public int SlotNumber { get; set; } // 1..10
        public string TimeRange { get; set; } = "";
    }

    public class ScheduleCellViewModel
    {
        public bool IsDisabled { get; set; }
        public string Course { get; set; } = "";
        public string Room { get; set; } = "";
        public string Instructor { get; set; } = "";

        public int? MeetingId { get; set; }
        public int MeetingSlotLength { get; set; }
        public bool IsMeetingStart { get; set; }
    }

    public class ScheduleAssignmentListItem
    {
        public int AssignmentId { get; set; }
        public string Course { get; set; } = "";
        public string Instructor { get; set; } = "";
        public string Room { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class ScheduleGridViewModel
    {
        public int? OfferingId { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public bool IsExtension { get; set; }

        public string? ReturnUrl { get; set; }

        public int? PublicationId { get; set; }
        public SchedulePublicationStatus? PublicationStatus { get; set; }

        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public string Department { get; set; } = "";
        public string Batch { get; set; } = "";

        public List<ScheduleDayViewModel> Days { get; set; } = new();
        public List<ScheduleSlotViewModel> Slots { get; set; } = new();

        // Key format: "{dayOfWeek}:{slotNumber}"
        public Dictionary<string, ScheduleCellViewModel> Cells { get; set; } = new();

        public List<ScheduleAssignmentListItem> Assignments { get; set; } = new();
    }

    // Read-only course list item for the public schedule viewer
    public class PublicScheduleCourseListItem
    {
        public string Course { get; set; } = "";
        public string Time { get; set; } = "";
        public string Room { get; set; } = "";
        public string Instructor { get; set; } = "";
    }

    public class PublicScheduleChangeLogItem
    {
        public DateTime ChangedAtUtc { get; set; }
        public string Title { get; set; } = "";
        public string? Details { get; set; }
        public string? ChangedBy { get; set; }
    }

    // Public-facing schedule view model (no editing, just filters + timetable + course list)
    public class PublicScheduleViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string Program { get; set; } = ""; // Department name

        public string ProgramType { get; set; } = ""; // "" = All, "Regular" or "Extension"

        public int? SectionId { get; set; }

        public List<string> AcademicYears { get; set; } = new();
        public List<string> Programs { get; set; } = new();
        public List<ScheduleSectionListItem> Sections { get; set; } = new();

        public List<ScheduleDayViewModel> Days { get; set; } = new();
        public List<ScheduleSlotViewModel> Slots { get; set; } = new();
        public Dictionary<string, ScheduleCellViewModel> Cells { get; set; } = new();

        public List<PublicScheduleCourseListItem> Courses { get; set; } = new();

        public List<PublicScheduleChangeLogItem> ChangeLogs { get; set; } = new();
    }

    public class InstructorScheduleMeetingListItem
    {
        public int MeetingId { get; set; }
        public string Course { get; set; } = "";
        public string Room { get; set; } = "";
        public string Section { get; set; } = "";
        public int DayOfWeek { get; set; }
        public int SlotStart { get; set; }
        public int SlotLength { get; set; }

        public bool HasPendingSwapRequest { get; set; }
    }

    public class InstructorWeeklyScheduleViewModel
    {
        public string InstructorId { get; set; } = "";
        public string InstructorName { get; set; } = "";

        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public int? PublicationId { get; set; }

        public List<ScheduleDayViewModel> Days { get; set; } = new();
        public List<ScheduleSlotViewModel> Slots { get; set; } = new();
        public Dictionary<string, ScheduleCellViewModel> Cells { get; set; } = new();

        public List<InstructorScheduleMeetingListItem> Meetings { get; set; } = new();
    }
}
