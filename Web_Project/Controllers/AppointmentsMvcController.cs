using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization; // Needed for JSON naming
using Web_Project.Models;

namespace Web_Project.Controllers
{
    [Authorize]
    public class AppointmentsMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AppointmentsMvcController(IHttpClientFactory httpClientFactory)
        { _httpClientFactory = httpClientFactory; }

        [HttpGet]
        public async Task<IActionResult> Book(int? serviceId)
        {
            var vm = new BookAppointmentVm
            {
                ServiceId = serviceId ?? 0,
                Date = DateTime.Today
            };

            await LoadPageData(vm); 

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentVm vm)
        {
            //  Validation
            if (vm.TrainerId == null)
            {
                ModelState.AddModelError("", "Trainer is required.");
                await LoadPageData(vm);
                return View(vm);
            }

            var clientApi = _httpClientFactory.CreateClient("WebApi");

           
            var startAt = vm.Date.Date.Add(vm.StartTime);
            var endAt = vm.Date.Date.Add(vm.EndTime);

            
            var resp = await clientApi.PostAsJsonAsync("api/Appointments/Book", new
            {
                TrainerId = vm.TrainerId,
                ServiceId = vm.ServiceId,
                StartAt = startAt,
                EndAt = endAt
            });

            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(MyAppointments));

            
            var body = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Booking failed: {body}");

           
            await LoadPageData(vm);
            return View(vm);
        }

     
        private async Task LoadPageData(BookAppointmentVm vm)
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            // Services
            var servicesResp = await client.GetAsync("api/Services/GetServices");
            if (servicesResp.IsSuccessStatusCode)
            {
                var services = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDto>>() ?? new();
                vm.Services = services.Select(s => new SelectListItem
                {
                    Value = s.ServiceID.ToString(),
                    Text = s.ServiceName,
                    Selected = s.ServiceID == vm.ServiceId
                }).ToList();
            }

            
            if (vm.ServiceId != 0)
            {
                vm.AvailableSlots = new List<AppointmentSlot>();
                var today = DateTime.Today;

                for (int i = 0; i < 7; i++)
                {
                    var testDate = today.AddDays(i);
                    
                    var availResp = await client.GetAsync($"api/Trainers/Available?date={testDate:yyyy-MM-dd}&serviceId={vm.ServiceId}");

                    if (availResp.IsSuccessStatusCode)
                    {
                        var trainers = await availResp.Content.ReadFromJsonAsync<List<TrainerDto>>() ?? new();

                        foreach (var t in trainers)
                        {
                            vm.AvailableSlots.Add(new AppointmentSlot
                            {
                                Date = testDate,
                                TrainerId = t.TrainerID,
                                TrainerName = t.TrainerName ?? "Unknown",
                                
                                StartTime = t.StartTime ?? "00:00",
                                EndTime = t.EndTime ?? "00:00"
                            });
                        }
                    }
                }
            }
        }




        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.GetAsync("api/Appointments/MyAppointments");

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return Redirect("/Identity/Account/Login");

            if (!resp.IsSuccessStatusCode)
                return View("Error");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiList = await resp.Content.ReadFromJsonAsync<List<AppointmentApiDto>>(options) ?? new();

            
            var vm = apiList
                .Select(a => new MyAppointmentRowVm
                {
                    AppointmentID = a.AppointmentID,
                    ServiceName = a.ServiceName,
                    TrainerName = a.TrainerName,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsApproved = a.IsApproved 
                })
                .OrderBy(a => a.StartTime)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.DeleteAsync($"api/Appointments/Cancel/{id}");

            if (resp.IsSuccessStatusCode)
            {
                TempData["Success"] = "Appointment cancelled successfully.";
            }
            else
            {
                TempData["Error"] = "Could not cancel appointment. It may be too late or already removed.";
            }

            return RedirectToAction(nameof(MyAppointments));
        }

    }

    
    public class ServiceDto
    {
        [JsonPropertyName("serviceID")] 
        public int ServiceID { get; set; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; set; }
    }

    public class TrainerDto
    {
        [JsonPropertyName("trainerID")]
        public int TrainerID { get; set; }

        [JsonPropertyName("trainerName")]
        public string? TrainerName { get; set; }

       
        [JsonPropertyName("startTime")]
        public string? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string? EndTime { get; set; }
    }

    public class AppointmentApiDto
    {
        [JsonPropertyName("appointmentID")]
        public int AppointmentID { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("trainerName")]
        public string? TrainerName { get; set; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; set; }

       
        [JsonPropertyName("isApproved")]
        public bool IsApproved { get; set; }
    }

   
    public class MyAppointmentRowVm
    {
        public int AppointmentID { get; set; }
        public string? ServiceName { get; set; }
        public string? TrainerName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // --- ADD THIS ---
        public bool IsApproved { get; set; }
    }
}