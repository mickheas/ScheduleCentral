namespace ScheduleCentral.Models.ViewModels
{
    public class ResourceManagementViewModel
    {
        public List<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public List<Room> Rooms { get; set; } = new List<Room>();
        public List<Course> Courses { get; set; } = new List<Course>();
    }
    public class ProgramOfficerOfferingListItem
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public OfferingStatus Status { get; set; }
        public bool HasSubmitted { get; set; }
        public bool HasFeedback { get; set; }
    }

    public class ProgramOfficerIndexViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "I";
        public List<ProgramOfficerOfferingListItem> Offerings { get; set; } = new();
    }

    public class ProgramOfficerApprovedHistoryItem
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public int BatchCount { get; set; }
        public bool HasSectionsGenerated { get; set; }
    }

    public class ProgramOfficerApprovedHistoryViewModel
    {
        public string? AcademicYear { get; set; }
        public string? Semester { get; set; }
        public string? Department { get; set; }
        public int? YearLevel { get; set; }
        public string? BatchName { get; set; }
        public List<ProgramOfficerApprovedHistoryItem> Offerings { get; set; } = new();
    }

    public class CreateSectionsBatchRowViewModel
    {
        public int OfferingBatchId { get; set; }
        public int YearLevel { get; set; }
        public string BatchName { get; set; } = "";
        public string Semester { get; set; } = "";
        public bool IsExtension { get; set; }
        public int SectionCount { get; set; } = 1;
    }

    public class CreateSectionsViewModel
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public List<CreateSectionsBatchRowViewModel> Batches { get; set; } = new();
    }
}