using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Web_Project.Controllers
{
    [Authorize]
    public class AccountSetupController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountSetupController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // POST: /AccountSetup/BecomeMember
        [HttpPost]
        public async Task<IActionResult> BecomeMember()
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            var resp = await client.PostAsync("api/Members/RegisterMe", null);

            if (resp.IsSuccessStatusCode || (int)resp.StatusCode == 409)
                return RedirectToAction("Index", "Home");

            var body = await resp.Content.ReadAsStringAsync();
            return Content($"API error: {(int)resp.StatusCode} - {body}");
        }

        // POST: /AccountSetup/BecomeTrainer
        [HttpPost]
        public async Task<IActionResult> BecomeTrainer(string expertiseAreas, string? description)
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            var resp = await client.PostAsJsonAsync("api/Trainers/RegisterMe", new
            {
                ExpertiseAreas = expertiseAreas,
                Description = description
            });

            if (resp.IsSuccessStatusCode || (int)resp.StatusCode == 409)
                return RedirectToAction("Index", "Home");

            var body = await resp.Content.ReadAsStringAsync();
            return Content($"API error: {(int)resp.StatusCode} - {body}");
        }
    }
}
