using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class BookAppointmentVm
{
    // We still need these for the form submission
    [Required]
    public int ServiceId { get; set; }

    public int? TrainerId { get; set; } // Selected Trainer

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } // Selected Date

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    // Dropdown for Services
    public List<SelectListItem> Services { get; set; } = new();

    // The list of "Opportunities" found for the next week
    public List<AppointmentSlot> AvailableSlots { get; set; } = new();
}

public class AppointmentSlot
{
    public DateTime Date { get; set; }
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // e.g. "09:00"
    public string EndTime { get; set; } = string.Empty;   // e.g. "17:00"
}