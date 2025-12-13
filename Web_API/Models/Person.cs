using System.ComponentModel.DataAnnotations;
using Web_API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace Web_API.Models
{
    [Table("People")]
    [Index(nameof(UserId), IsUnique = true)]
    public class Person
    {
        [Key]
        public int PersonID { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? Firstname { get; set; } 

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? Lastname { get; set; }


        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [MaxLength(15, ErrorMessage = "Phone number is too long.")]
        public string? Phone { get; set; }


        // Link to Identity (AspNetUsers.Id)
        [Required]
        [MaxLength(450)]
        public string? UserId { get; set; }


        public Person()
        {

        }

    }
}
