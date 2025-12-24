using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using Web_Project.Models;

namespace Web_Project.Controllers
{
    public class TrainersMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TrainersMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.GetAsync("api/Trainers/GetTrainers");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


                var apiTrainers = await response.Content.ReadFromJsonAsync<List<ApiTrainer>>(options);

                // We map it to a View Model (TrainerDTO) for display
                var dtoList = apiTrainers.Select(t => new TrainerDTO
                {
                    TrainerID = t.TrainerID,
                    PersonID = t.PersonID,
                    FirstName = t.Person?.FirstName,
                    LastName = t.Person?.LastName,
                    // Use the existing Description/Expertise
                    Description = t.Description,
                    ExpertiseAreas = t.ExpertiseAreas,

                    // Map the skills list so we can show what services they provide
                    AssignedSkills = t.Skills?.Select(s => new TrainerSkillItem
                    {
                        ServiceName = s.Service?.ServiceName ?? "Unknown Service"
                    }).ToList() ?? new List<TrainerSkillItem>()

                }).ToList();

                return View(dtoList);
            }

            return View(new List<TrainerDTO>());
        }
    }
}