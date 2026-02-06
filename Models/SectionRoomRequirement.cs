using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleCentral.Models
{
    public class SectionRoomRequirement
    {
        public int Id { get; set; }

        public int CourseOfferingSectionId { get; set; }
        public CourseOfferingSection CourseOfferingSection { get; set; } = null!;

        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; } 

        [Range(1, 20)]
        public int HoursPerWeek { get; set; }
    }
}