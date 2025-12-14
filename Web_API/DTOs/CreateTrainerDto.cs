using System.ComponentModel.DataAnnotations;

namespace Web_API.DTOs
{
    public class CreateTrainerDto
    {
        [Required, MaxLength(200)]
        public string? ExpertiseAreas { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
