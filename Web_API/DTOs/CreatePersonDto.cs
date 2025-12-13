using System.ComponentModel.DataAnnotations;

namespace Web_API.DTOs
{
    public class CreatePersonDto
    {

        [Required, MaxLength(50)]
        public string? Firstname { get; set; } 

        [Required, MaxLength(50)]
        public string? Lastname { get; set; }

        [Required, Phone, MaxLength(15)]
        public string? Phone { get; set; }
    }
}
