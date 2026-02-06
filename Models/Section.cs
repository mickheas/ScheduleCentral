using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models
{
    // Represents a subdivision of a batch (e.g., "Section A", "Extension Group 1")
    public class Section
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } // e.g., "Section A", "Section B"
        public int NumberOfStudents { get; set; }
        public bool IsExtension { get; set; } = false; // Regular vs Extension

        public int BatchId { get; set; }
        public Batch Batch { get; set; }
    }
}