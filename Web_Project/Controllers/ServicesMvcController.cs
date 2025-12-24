using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using Web_Project.Models;

namespace Web_Project.Controllers
{
    // Note: No [Authorize] attribute here, so ANYONE can see this page.
    public class ServicesMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ServicesMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            
            var response = await client.GetAsync("api/Services/GetServices");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var services = await response.Content.ReadFromJsonAsync<List<ServiceDTO>>(options);
                return View(services);
            }

            
            return View(new List<ServiceDTO>());
        }
    }
}