using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Web_API.DTOs
{
    public class BookAppointmentDto
    {
        [JsonPropertyName("trainerId")]
        public int TrainerId { get; set; }

        [JsonPropertyName("serviceId")]
        public int ServiceId { get; set; }

        [JsonPropertyName("startAt")]
        public DateTime StartAt { get; set; }

        [JsonPropertyName("endAt")]
        public DateTime EndAt { get; set; }
    }
}
