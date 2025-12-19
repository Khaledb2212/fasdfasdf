using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Web_Project.Models
{
    public class TrainerDTO
    {

        [JsonPropertyName("trainerID")]
        public int TrainerID { get; set; }

        [JsonPropertyName("personID")]
        public int PersonID { get; set; }



        [Required(ErrorMessage ="Admin Email bilemez, Trainer gelsin")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        [JsonPropertyName("email")] 
        public string? Email { get; set; }


        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        [Required]
        [Phone]
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [Required]
        [Display(Name = "Expertise (e.g. Yoga, Pilates)")]
        [JsonPropertyName("expertiseAreas")]
        public string? ExpertiseAreas { get; set; }

        [Required]
        [Display(Name = "Description")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}