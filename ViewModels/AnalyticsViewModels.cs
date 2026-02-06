using System.Collections.Generic;

namespace ScheduleCentral.Models.ViewModels
{
    public class AnalyticsDashboardViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public List<string> AcademicYears { get; set; } = new();
        public List<string> Semesters { get; set; } = new();

        public string PrimaryRole { get; set; } = "";
        public string? ScopeDepartment { get; set; }

        public int TotalOfferings { get; set; }
        public int ApprovedOfferings { get; set; }
        public int RejectedOfferings { get; set; }
        public int SubmittedOfferings { get; set; }
        public int DraftOfferings { get; set; }
        public double RejectionRatePercent { get; set; }

        public int TotalMeetings { get; set; }
        public int RoomsUsed { get; set; }
        public int InstructorsUsed { get; set; }
        public double RoomUtilizationPercent { get; set; }

        public int ScheduleChanges { get; set; }
        public double ChangesPerMeeting { get; set; }

        public int MyMeetings { get; set; }
        public int MyScheduledSlots { get; set; }
        public int MyAvailableHours { get; set; }
        public double MyLoadUtilizationPercent { get; set; }

        public string OfferingsStatusLabelsJson { get; set; } = "[]";
        public string OfferingsStatusDataJson { get; set; } = "[]";

        public string TopRoomsLabelsJson { get; set; } = "[]";
        public string TopRoomsDataJson { get; set; } = "[]";

        public string ChangesOverTimeLabelsJson { get; set; } = "[]";
        public string ChangesOverTimeDataJson { get; set; } = "[]";

        public string MeetingsByDayLabelsJson { get; set; } = "[]";
        public string MeetingsByDayDataJson { get; set; } = "[]";

        public string MeetingsByDepartmentLabelsJson { get; set; } = "[]";
        public string MeetingsByDepartmentDataJson { get; set; } = "[]";
    }
}
