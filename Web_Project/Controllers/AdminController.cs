using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        // 1. DASHBOARD
        [HttpGet]
        public IActionResult Index()
        { 
            return View();
        }


        // 2. LIST TRAINERS
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

        // 3. CREATE TRAINER (GET)
        [HttpGet]
        public IActionResult CreateTrainer()
        { 
            return View(); 
        }

        // 3. CREATE TRAINER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(TrainerDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            // A. Create Identity User
            var user = new UserDetails { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(dto);
            }

            await _userManager.AddToRoleAsync(user, "Trainer");

            // B. Create Person via API
            var client = _httpClientFactory.CreateClient("WebApi");
            var personData = new { UserId = user.Id, Firstname = dto.FirstName, Lastname = dto.LastName, Phone = dto.Phone, Email = dto.Email };

            var personResp = await client.PostAsJsonAsync("api/People/PostPerson", personData);
            if (!personResp.IsSuccessStatusCode)
            {
                await _userManager.DeleteAsync(user); // Cleanup
                ModelState.AddModelError("", "Failed to save Person details.");
                return View(dto);
            }

            var createdPerson = await personResp.Content.ReadFromJsonAsync<PersonIdHelper>();

            // C. Create Trainer via API
            var trainerData = new { PersonID = createdPerson.PersonID, ExpertiseAreas = dto.ExpertiseAreas, Description = dto.Description };
            var trainerResp = await client.PostAsJsonAsync("api/Trainers/AddTrainer", trainerData);

            if (trainerResp.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Trainer {dto.FirstName} created!";
                return RedirectToAction(nameof(Trainers));
            }

            ModelState.AddModelError("", "Failed to save Trainer details.");
            return View(dto);
        }

      
        // 4. EDIT TRAINER (GET)
        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            // Correct URL with query string
            var response = await client.GetAsync($"api/Trainers/GetTrainer?id={id}");

            if (!response.IsSuccessStatusCode) return NotFound();

            var item = await response.Content.ReadFromJsonAsync<JsonElement>();

            // HELPER: Try to get property with Upper OR Lower case to avoid crashes
            string GetStr(JsonElement el, string key)
            {
                if (el.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String) return val.GetString();
                if (el.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out var val2) && val2.ValueKind == JsonValueKind.String) return val2.GetString();
                return "";
            }

            int GetInt(JsonElement el, string key)
            {
                if (el.TryGetProperty(key, out var val)) return val.GetInt32();
                if (el.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out var val2)) return val2.GetInt32();
                return 0;
            }

            // Manual Map: API JSON -> TrainerDTO
            var dto = new TrainerDTO
            {
                TrainerID = GetInt(item, "trainerID"),
                ExpertiseAreas = GetStr(item, "expertiseAreas"),
                Description = GetStr(item, "description")
            };

            // Safely handle nested Person data
            JsonElement person;
            if (item.TryGetProperty("person", out person) || item.TryGetProperty("Person", out person))
            {
                dto.PersonID = GetInt(person, "personID");
                dto.FirstName = GetStr(person, "firstname");
                dto.LastName = GetStr(person, "lastname");
                dto.Phone = GetStr(person, "phone");

                // CRITICAL FIX: Don't crash if Email is missing
                var email = GetStr(person, "email");
                dto.Email = string.IsNullOrEmpty(email) ? "Registered User (Identity)" : email;
            }

            return View(dto);
        }

        // 5. EDIT TRAINER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(TrainerDTO dto)
        {
            // IMPORTANT: Remove Password validation because we don't require it for updates
            ModelState.Remove("Password");

            if (!ModelState.IsValid) return View(dto);

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
            {
                TempData["Success"] = "Trainer updated successfully!";
                return RedirectToAction(nameof(Trainers));
            }

            ModelState.AddModelError("", "Failed to update trainer.");
            return View(dto);
        }

        // 6. DELETE TRAINER
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

        // 7. TRAINER DETAILS (GET)
        [HttpGet]
        public async Task<IActionResult> TrainerDetails(int id)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.GetAsync($"api/Trainers/GetTrainer?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Trainer not found.";
                return RedirectToAction(nameof(Trainers));
            }

            // 1. Read Raw JSON to handle nested objects safely
            var item = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

            // 2. Manual Map: JSON -> TrainerDTO
            var dto = new TrainerDTO
            {
                // Root properties
                TrainerID = item.TryGetProperty("trainerID", out var tId) ? tId.GetInt32() : 0,
                ExpertiseAreas = item.TryGetProperty("expertiseAreas", out var exp) ? exp.GetString() : "",
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : ""
            };

            // Nested Person properties
            if (item.TryGetProperty("person", out var person))
            {
                dto.PersonID = person.TryGetProperty("personID", out var pId) ? pId.GetInt32() : 0;
                dto.FirstName = person.TryGetProperty("firstname", out var fn) ? fn.GetString() : "Unknown";
                dto.LastName = person.TryGetProperty("lastname", out var ln) ? ln.GetString() : "";
                dto.Phone = person.TryGetProperty("phone", out var ph) ? ph.GetString() : "";

                // Safe Email Check (Prevents crash if email is only in Identity DB)
                if (person.TryGetProperty("email", out var em) && em.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    dto.Email = em.GetString();
                }
                else
                {
                    dto.Email = "Registered User (Identity)";
                }
            }

            return View(dto);
        }

        // Helper class for Person ID response
        public class PersonIdHelper { public int PersonID { get; set; } }
    }
}