using System;
using System.Collections.Generic;

namespace ScheduleCentral.Models.ViewModels
{
    public class ScheduleMeetingHistoryListItem
    {
        public int MeetingId { get; set; }
        public int? SectionId { get; set; }
        public bool IsExtension { get; set; }
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public int? PublicationId { get; set; }

        public string Department { get; set; } = "";
        public string Batch { get; set; } = "";
        public string Section { get; set; } = "";

        public string Course { get; set; } = "";
        public string Instructor { get; set; } = "";
        public string Room { get; set; } = "";

        public int DayOfWeek { get; set; }
        public string DayLabel { get; set; } = "";
        public int SlotStart { get; set; }
        public int SlotLength { get; set; }
        public string TimeRange { get; set; } = "";
    }

    public class ScheduleMeetingsHistoryViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public int? PublicationId { get; set; }

        public string? Department { get; set; }
        public string? InstructorId { get; set; }
        public int? RoomId { get; set; }

        public List<string> AcademicYears { get; set; } = new();
        public List<string> Semesters { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public List<(string Id, string Name)> Instructors { get; set; } = new();
        public List<(int Id, string Name)> Rooms { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }

        public List<ScheduleMeetingHistoryListItem> Meetings { get; set; } = new();
    }

    public class ScheduleAuditLogItem
    {
        public DateTime ChangedAtUtc { get; set; }
        public string ChangedAtLocal { get; set; } = "";

        public string? ChangedByUserId { get; set; }
        public string ChangedBy { get; set; } = "";

        public int PublicationId { get; set; }
        public int ScheduleMeetingId { get; set; }
        public int? SectionId { get; set; }

        public string ChangeType { get; set; } = "";

        public int OldDayOfWeek { get; set; }
        public int OldSlotStart { get; set; }
        public int NewDayOfWeek { get; set; }
        public int NewSlotStart { get; set; }

        public string OldLabel { get; set; } = "";
        public string NewLabel { get; set; } = "";

        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Course { get; set; }
        public string? Instructor { get; set; }
        public string? Room { get; set; }
    }

    public class ScheduleAuditViewModel
    {
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";

        public int? PublicationId { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangeType { get; set; }

        public List<string> AcademicYears { get; set; } = new();
        public List<string> Semesters { get; set; } = new();
        public List<(int Id, string Label)> Publications { get; set; } = new();
        public List<(string Id, string Name)> Users { get; set; } = new();
        public List<string> ChangeTypes { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }

        public List<ScheduleAuditLogItem> Logs { get; set; } = new();
    }
}
