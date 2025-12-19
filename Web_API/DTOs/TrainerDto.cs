namespace Web_API.DTOs
{
    public class TrainerDto
    {
        public int TrainerID { get; set; }
        public string? TrainerName { get; set; }
        public string? StartTime { get; set; } // The API returns these as strings formatted "HH:mm"
        public string? EndTime { get; set; }
    }
}
