using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;
using Web_Project.Models;


namespace Web_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<UserDetails> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(UserManager<UserDetails> userManager, IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
        }

      
        [HttpGet]
        public IActionResult Index()
        { 
            return View();
        }


      
        [HttpGet]
        public async Task<IActionResult> Trainers()
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.GetAsync("api/Trainers/GetTrainers");

            if (response.IsSuccessStatusCode)
            {
                var rawData = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
                var trainers = new List<TrainerDTO>();

                foreach (var item in rawData)
                {
                    // HELPER: Try to get property with Upper OR Lower case
                    int GetInt(string key)
                    {
                        if (item.TryGetProperty(key, out var val)) return val.GetInt32();
                        if (item.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out var val2)) return val2.GetInt32();
                        return 0;
                    }

                    string GetStr(string key)
                    {
                        if (item.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String) return val.GetString();
                        if (item.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out var val2) && val2.ValueKind == JsonValueKind.String) return val2.GetString();
                        return "";
                    }

                    var dto = new TrainerDTO
                    {
                        // Now handles both "trainerID" and "TrainerID"
                        TrainerID = GetInt("trainerID"),
                        ExpertiseAreas = GetStr("expertiseAreas"),
                        Description = GetStr("description")
                    };

                    // Handle Nested Person Object safely
                    JsonElement person;
                    if (item.TryGetProperty("person", out person) || item.TryGetProperty("Person", out person))
                    {
                        // Helper for nested object properties
                        int GetPersonInt(string key) =>
                            (person.TryGetProperty(key, out var v) || person.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out v)) ? v.GetInt32() : 0;

                        string GetPersonStr(string key) =>
                            (person.TryGetProperty(key, out var v) || person.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out v)) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

                        dto.PersonID = GetPersonInt("personID");
                        dto.FirstName = GetPersonStr("firstname");
                        dto.LastName = GetPersonStr("lastname");
                        dto.Phone = GetPersonStr("phone");

                        // Safe Email Check
                        var emailJson = person.TryGetProperty("email", out var e) ? e : (person.TryGetProperty("Email", out var e2) ? e2 : default);
                        dto.Email = (emailJson.ValueKind == JsonValueKind.String) ? emailJson.GetString() : "Registered User (Identity)";
                    }

                    trainers.Add(dto);
                }

                return View(trainers);
            }

            return View(new List<TrainerDTO>());
        }

       

   
        [HttpGet]
        public async Task<IActionResult> CreateTrainer()
        {
            var model = new TrainerDTO();
            var client = _httpClientFactory.CreateClient("WebApi");

            // Fetch Services to populate the checkboxes
            var servicesResp = await client.GetAsync("api/Services/GetServices");
            if (servicesResp.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allServices = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>(options);

                model.AvailableServices = allServices?.Select(s => new SelectListItem
                {
                    Value = s.ServiceID.ToString(),
                    Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
                }).ToList();
            }

            return View(model);
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(TrainerDTO dto)
        {
           
            if (!ModelState.IsValid)
            {
                return await CreateTrainer();
            }

            // Create Identity User
            var user = new UserDetails { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return await CreateTrainer();
            }

            await _userManager.AddToRoleAsync(user, "Trainer");


            var client = _httpClientFactory.CreateClient("WebApi");
            var personDto = new
            {

                // Donot use User.FindFirstValue That is YOU (The Admin)
                //That is the NEW TRAINER
                UserId = user.Id,
                Firstname = dto.FirstName,
                Lastname = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email
            };


            var personResp = await client.PostAsJsonAsync("api/People/PostPerson", personDto);

           

            if (!personResp.IsSuccessStatusCode)
            {
                //Delete the user we just created
                await _userManager.DeleteAsync(user);

               
                var errorContent = await personResp.Content.ReadAsStringAsync();

               
                ModelState.AddModelError("", $"API Error: {personResp.StatusCode} - {errorContent}");
                return await CreateTrainer();
            }

            var createdPerson = await personResp.Content.ReadFromJsonAsync<PersonIdHelper>();

          
            var trainerData = new { PersonID = createdPerson.PersonID, ExpertiseAreas = dto.ExpertiseAreas, Description = dto.Description };
            var trainerResp = await client.PostAsJsonAsync("api/Trainers/AddTrainer", trainerData);

            if (trainerResp.IsSuccessStatusCode)
            {
                //Skills (Checkboxes)
                if (dto.SelectedServiceIds != null && dto.SelectedServiceIds.Any())
                {
                    var createdTrainerJson = await trainerResp.Content.ReadFromJsonAsync<JsonElement>();

                    int newTrainerId = createdTrainerJson.TryGetProperty("trainerID", out var idVal) ? idVal.GetInt32() :
                                      (createdTrainerJson.TryGetProperty("TrainerID", out var idVal2) ? idVal2.GetInt32() : 0);

                    if (newTrainerId > 0)
                    {
                        foreach (var serviceId in dto.SelectedServiceIds)
                        {
                            var skillPayload = new { TrainerId = newTrainerId, ServiceId = serviceId };
                            await client.PostAsJsonAsync("api/TrainerSkills/PostTrainerSkill", skillPayload);
                        }
                    }
                }

                TempData["Success"] = $"Trainer {dto.FirstName} created with skills!";
                return RedirectToAction(nameof(Trainers));
            }

            
            var trainerError = await trainerResp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Failed to save Trainer details. API says: {trainerError}");
            return await CreateTrainer();
        }

      

        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");

   
            var response = await client.GetAsync($"api/Trainers/GetTrainer?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Trainer not found (API Error: {response.StatusCode})");
            }

        
            var apiTrainer = await response.Content.ReadFromJsonAsync<Web_Project.Models.ApiTrainer>();

            if (apiTrainer == null) return NotFound();

            var services = new List<ServiceDTO>();
            var servicesResp = await client.GetAsync("api/Services/GetServices"); 
            if (servicesResp.IsSuccessStatusCode)
            {
                services = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>() ?? new List<ServiceDTO>();
            }

    
            var dto = new TrainerDTO
            {
                TrainerID = apiTrainer.TrainerID,
                PersonID = apiTrainer.PersonID,

                // Map Person info (Flattening the structure)
                FirstName = apiTrainer.Person?.FirstName,
                LastName = apiTrainer.Person?.LastName,
                Phone = apiTrainer.Person?.Phone,
                Email = apiTrainer.Person?.Email,

                
                ExpertiseAreas = apiTrainer.ExpertiseAreas,
                Description = apiTrainer.Description,

                
                AssignedSkills = apiTrainer.Skills?.Select(s => new TrainerSkillItem
                {
                    Id = s.TrainerSkillID,      
                    ServiceId = s.ServiceID,    
                    ServiceName = s.Service?.ServiceName ?? "Unknown" 
                }).ToList() ?? new List<TrainerSkillItem>(),

                // dropdown list
                AvailableServices = services.Select(s => new SelectListItem
                {
                    Value = s.ServiceID.ToString(),
                    Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
                })
            };

            return View(dto);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(TrainerDTO dto)
        {
            ModelState.Remove("Password");
            ModelState.Remove("AssignedSkills"); 

            if (!ModelState.IsValid) return await EditTrainer(dto.TrainerID); 

            var client = _httpClientFactory.CreateClient("WebApi");
            var updateDto = new
            {
                TrainerID = dto.TrainerID,
                PersonID = dto.PersonID,
                ExpertiseAreas = dto.ExpertiseAreas,
                Description = dto.Description,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone
            };

            var response = await client.PutAsJsonAsync($"api/Trainers/UpdateTrainer?id={dto.TrainerID}", updateDto);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Profile updated.";
            else
                TempData["Error"] = "Update failed.";

            
            return RedirectToAction(nameof(EditTrainer), new { id = dto.TrainerID });
        }

      

        [HttpPost]
        public async Task<IActionResult> AddSkillToTrainer(int trainerId, int serviceId)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var payload = new { TrainerId = trainerId, ServiceId = serviceId };

            var response = await client.PostAsJsonAsync("api/TrainerSkills/PostTrainerSkill", payload);

            if (response.IsSuccessStatusCode) TempData["Success"] = "Skill added!";
            else TempData["Error"] = "Could not add skill.";

            return RedirectToAction(nameof(EditTrainer), new { id = trainerId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSkillFromTrainer(int skillId, int trainerId)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.DeleteAsync($"api/TrainerSkills/DeleteTrainerSkill?id={skillId}");

            if (response.IsSuccessStatusCode) TempData["Success"] = "Skill removed.";
            else TempData["Error"] = "Could not remove skill.";

            return RedirectToAction(nameof(EditTrainer), new { id = trainerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.DeleteAsync($"api/Trainers/DeleteTrainer?id={id}");

            if (response.IsSuccessStatusCode) TempData["Success"] = "Deleted successfully.";
            else TempData["Error"] = "Delete failed.";

            return RedirectToAction(nameof(Trainers));
        }

       
        [HttpGet]
        public async Task<IActionResult> TrainerDetails(int id)
        {

            var client = _httpClientFactory.CreateClient("WebApi");

            // ---------------------------------------------------------
            // 1. Fetch the Trainer Data
            // ---------------------------------------------------------
            var response = await client.GetAsync($"api/Trainers/GetTrainer?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Trainer not found (API Error: {response.StatusCode})");
            }

            // USE THE NEW HELPER CLASS HERE:
            // We read into 'ApiTrainer' because it matches the nested JSON structure.
            var apiTrainer = await response.Content.ReadFromJsonAsync<Web_Project.Models.ApiTrainer>();

            if (apiTrainer == null) return NotFound();

            // ---------------------------------------------------------
            // 2. Fetch All Services (for the "Add Skill" dropdown)
            // ---------------------------------------------------------
            var services = new List<ServiceDTO>();
            var servicesResp = await client.GetAsync("api/Services/GetServices");
            if (servicesResp.IsSuccessStatusCode)
            {
                services = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>() ?? new List<ServiceDTO>();
            }

            // ---------------------------------------------------------
            // 3. MAP Nested Data -> Flat DTO
            // ---------------------------------------------------------
            var dto = new TrainerDTO
            {
                TrainerID = apiTrainer.TrainerID,
                PersonID = apiTrainer.PersonID,

                // Map Person info (Flattening the structure)
                FirstName = apiTrainer.Person?.FirstName,
                LastName = apiTrainer.Person?.LastName,
                Phone = apiTrainer.Person?.Phone,
                Email = apiTrainer.Person?.Email,

                // Map Trainer info
                ExpertiseAreas = apiTrainer.ExpertiseAreas,
                Description = apiTrainer.Description,

                // Map Skills (This fixes the "Invisible Skills" issue)
                AssignedSkills = apiTrainer.Skills?.Select(s => new TrainerSkillItem
                {
                    Id = s.TrainerSkillID,       // The Link ID (needed for delete)
                    ServiceId = s.ServiceID,     // The Service ID
                    ServiceName = s.Service?.ServiceName ?? "Unknown" // The Name
                }).ToList() ?? new List<TrainerSkillItem>(),

                // Populate the dropdown list
                AvailableServices = services.Select(s => new SelectListItem
                {
                    Value = s.ServiceID.ToString(),
                    Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
                })
            };

            return View(dto);
        }
        
        
    
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.GetAsync("api/Services/GetServices");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var services = await response.Content.ReadFromJsonAsync<List<ServiceDTO>>(options) ?? new();
                return View(services);
            }

            return View(new List<ServiceDTO>());
        }

        
        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(ServiceDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var client = _httpClientFactory.CreateClient("WebApi");

            // API expects POST to "api/Services/PostService"
            var response = await client.PostAsJsonAsync("api/Services/PostService", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service created successfully!";
                return RedirectToAction(nameof(Services));
            }

            ModelState.AddModelError("", "Failed to create service.");
            return View(dto);
        }

        
        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            
            var response = await client.GetAsync($"api/Services/GetService?id={id}");

            if (!response.IsSuccessStatusCode) return NotFound();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var service = await response.Content.ReadFromJsonAsync<ServiceDTO>(options);

            return View(service);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(ServiceDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var client = _httpClientFactory.CreateClient("WebApi");

         
            var response = await client.PutAsJsonAsync($"api/Services/PutService?id={dto.ServiceID}", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service updated successfully!";
                return RedirectToAction(nameof(Services));
            }

            ModelState.AddModelError("", "Failed to update service.");
            return View(dto);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            
            var response = await client.DeleteAsync($"api/Services/DeleteService?id={id}");

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Service deleted.";
            else
                TempData["Error"] = "Failed to delete service.";

            return RedirectToAction(nameof(Services));
        }

       
        [HttpGet]
        public async Task<IActionResult> ServiceDetails(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.GetAsync($"api/Services/GetService?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Service not found.";
                return RedirectToAction(nameof(Services));
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var service = await response.Content.ReadFromJsonAsync<ServiceDTO>(options);

            return View(service);
        }

       
        public class PersonIdHelper { public int PersonID { get; set; } }
    }
}