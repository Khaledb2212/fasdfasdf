using System.ComponentModel.DataAnnotations;

namespace Web_API.DTOs
{
    public class AddAvailabilityDto
    {
        [Range(0, 6)]
        public int DayOfWeek { get; set; }   // 0..6

        [Required]
        public DateTime StartTime { get; set; }   // 09:00

        [Required]
        public DateTime EndTime { get; set; }     // 12:00


        [Required]
        public int ServiceTypeId { get; set; }        // ServiceID
    }
}
