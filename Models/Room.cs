using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        //[Display(Name = "Room Number/Name")]
        public string Name { get; set; } // e.g., "B204", "Computer Lab 1"

        [Required]
        public int Capacity { get; set; }

        [Required]
        public int RoomTypeId { get; set; }
        public RoomType? RoomType { get; set; }
    }
}