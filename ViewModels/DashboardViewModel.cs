namespace ScheduleCentral.Models.ViewModels
{
    public class DashboardViewModel
    {
        // User Stats
        public int TotalUsers { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalStudents { get; set; }
        public int PendingApprovals { get; set; }

        // System / Offering Stats
        public int TotalDepartments { get; set; }
        public int TotalCourses { get; set; }
        public int TotalOfferings { get; set; }
        public int ApprovedOfferings { get; set; }
        public int TotalSections { get; set; }
        public int TotalScheduleGrids { get; set; }
        public int TotalMeetings { get; set; }

        // Offerings grouped by status (for charts)
        public IDictionary<string, int> OfferingsByStatus { get; set; } = new Dictionary<string, int>();

        // Instructor-specific stats for personalized dashboard
        public int MyAvailableHours { get; set; }
        public int MyCurrentLoad { get; set; }
        public int MyAssignedSections { get; set; }
        public int MyAvailabilitySlotsSelected { get; set; }

        // Current User Context
        public string UserName { get; set; }
        public IList<string> UserRoles { get; set; }
        public bool IsAuthenticated { get; set; }

        // Primary role for dashboard look & feel (Department > TopManagement > ProgramOfficer > Instructor > Student)
        public string PrimaryRole { get; set; }

        public int UnreadNotifications { get; set; }
        public List<ScheduleCentral.Models.UserNotification> RecentNotifications { get; set; } = new();
    }
}