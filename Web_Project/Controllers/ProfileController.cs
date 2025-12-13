using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Web_Project.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProfileController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> FinishRegistration()
        {
            // values come from TempData (set during register)
            var firstname = TempData["Firstname"] as string;
            var lastname = TempData["Lastname"] as string;
            var phone = TempData["Phone"] as string;

            if (string.IsNullOrWhiteSpace(firstname) ||
                string.IsNullOrWhiteSpace(lastname) ||
                string.IsNullOrWhiteSpace(phone))
            {
                return RedirectToAction("Index", "Home");
            }

            var client = _httpClientFactory.CreateClient("WebApi");

            var resp = await client.PostAsJsonAsync("api/People/PostPerson", new
            {
                Firstname = firstname,
                Lastname = lastname,
                Phone = phone
            });

            // If already exists, it's fine
            if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
            {
                return Content($"API error creating Person: {(int)resp.StatusCode}");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
