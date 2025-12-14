using System.ComponentModel.DataAnnotations;

namespace Web_API.DTOs
{
    public class BookAppointmentDto
    {
        [Required]
        public int TrainerId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }
    }
}
