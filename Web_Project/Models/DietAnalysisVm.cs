using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Web_Project.Models
{
    public class DietAnalysisVm
    {
 
        [Display(Name = "Upload Body Photo")]
        public IFormFile? Photo { get; set; }

        [Display(Name = "Height (cm)")]
        public double? Height { get; set; }

        [Display(Name = "Weight (kg)")]
        public double? Weight { get; set; }

        [Display(Name = "Age")]
        public int? Age { get; set; }

        // New: user chooses whether to generate the preview image
        [Display(Name = "Generate Future Preview Image")]
        public bool GeneratePreviewImage { get; set; }

        // OUTPUT
        public string? AnalysisResult { get; set; }          // Diet & exercise plan (text)
        public string? ImageUrl { get; set; }                // Uploaded image path
        public string? FuturePreviewImageUrl { get; set; }   // Generated "after" image path
    }
}
