using System.ComponentModel.DataAnnotations;

namespace Web_API.DTOs
{
    public class AddMySkillDto
    {
        [Required]
        public int ServiceId { get; set; }
    }
}
