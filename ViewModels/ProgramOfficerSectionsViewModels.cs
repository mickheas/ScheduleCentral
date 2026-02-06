using System.Collections.Generic;

namespace ScheduleCentral.Models.ViewModels
{
    public class ProgramOfficerSectionListItem
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public bool IsExtension { get; set; }
        public string BatchName { get; set; } = "";
    }

    public class ProgramOfficerSectionsViewModel
    {
        public int OfferingId { get; set; }
        public string Department { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public List<ProgramOfficerSectionListItem> Sections { get; set; } = new();
    }
}
