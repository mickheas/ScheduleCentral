using System.ComponentModel.DataAnnotations;
namespace ScheduleCentral.Models
{

    public class CourseOfferingYearLevel
    {
        public int Id { get; set; }

        public int CourseOfferingId { get; set; }
        public CourseOffering CourseOffering { get; set; }

        // Numeric storage: 1,2,3,4...
        [Range(1, 6)]
        public int YearLevel { get; set; }

        public string DisplayYear =>
            YearLevel == 1 ? "1st Year" :
            YearLevel == 2 ? "2nd Year" :
            YearLevel == 3 ? "3rd Year" :
            $"{YearLevel}th Year";

        public string ProgramType { get; set; } = "Regular"; // or "Extension"

        public List<CourseOfferingBatch> Batches { get; set; } = new();
    }
}