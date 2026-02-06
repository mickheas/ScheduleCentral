using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public class ScheduleMeeting
    {
        public int Id { get; set; }

        public int? SchedulePublicationId { get; set; }
        public SchedulePublication? SchedulePublication { get; set; }

        [Required]
        public int ScheduleGridId { get; set; }
        public ScheduleGrid ScheduleGrid { get; set; } = null!;

        [Required]
        public string AcademicYear { get; set; } = "";

        [Required]
        public string Semester { get; set; } = "";

        [Required]
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public string? InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public ApplicationUser? Instructor { get; set; }

        public int? RoomId { get; set; }
        public Room? Room { get; set; }

        // 1=Mon,2=Tue,3=Wed,4=Thu,5=Fri,6=Sat,7=Sun
        [Range(1, 7)]
        public int DayOfWeek { get; set; }

        // 1..10 as per the slot mapping
        [Range(1, 10)]
        public int SlotStart { get; set; }

        // number of consecutive slots occupied, typically 1 or 2
        [Range(1, 10)]
        public int SlotLength { get; set; } = 1;
    }
}
