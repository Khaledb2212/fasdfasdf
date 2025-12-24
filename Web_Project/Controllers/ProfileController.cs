using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

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

        public async Task<IActionResult> FinishRegistration()
        {
            
            if (TempData["Firstname"] == null)
            {
               
                return RedirectToAction("Index", "Home");
            }

            var firstName = TempData["Firstname"]?.ToString();
            var lastName = TempData["Lastname"]?.ToString();
            var phone = TempData["Phone"]?.ToString();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 

            // Keep TempData for next request if needed (optional)
            TempData.Keep();

            var client = _httpClientFactory.CreateClient("WebApi");

            var personDto = new
            {
                UserId = userId,
                Firstname = firstName,
                Lastname = lastName,
                Phone = phone,
                Email = User.Identity.Name // distinct email if needed
            };

            var personResp = await client.PostAsJsonAsync("api/People/PostPerson", personDto);

            if (!personResp.IsSuccessStatusCode && personResp.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                var error = await personResp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create profile: {error}");
                return View("Error"); // Make sure you have an Error view or redirect
            }

       

            var memberResp = await client.PostAsync("api/Members/RegisterMe", null);

            if (!memberResp.IsSuccessStatusCode && memberResp.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
         
            }

            // 3. Done! Redirect to Dashboard
            return RedirectToAction("Index", "Home");
        }
    }
}