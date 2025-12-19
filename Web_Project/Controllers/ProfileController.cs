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
            // 1. Retrieve data passed from Register Page
            if (TempData["Firstname"] == null)
            {
                // If data is lost, redirect to home or show error
                return RedirectToAction("Index", "Home");
            }

            var firstName = TempData["Firstname"]?.ToString();
            var lastName = TempData["Lastname"]?.ToString();
            var phone = TempData["Phone"]?.ToString();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get from Cookie

            // Keep TempData for next request if needed (optional)
            TempData.Keep();

            var client = _httpClientFactory.CreateClient("WebApi");

            // -----------------------------------------------------------
            // STEP 1: Create the PERSON Record
            // -----------------------------------------------------------
            var personDto = new
            {
                UserId = userId,
                Firstname = firstName,
                Lastname = lastName,
                Phone = phone,
                Email = User.Identity.Name // distinct email if needed
            };

            var personResp = await client.PostAsJsonAsync("api/People/PostPerson", personDto);

            // If Person already exists or created successfully, we proceed.
            // (We assume success if it's 201 Created or 409 Conflict if they hit refresh)
            if (!personResp.IsSuccessStatusCode && personResp.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                var error = await personResp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create profile: {error}");
                return View("Error"); // Make sure you have an Error view or redirect
            }

            // -----------------------------------------------------------
            // STEP 2: Create the MEMBER Record (The Missing Piece!)
            // -----------------------------------------------------------
            // This calls the API method you showed me earlier: api/Members/RegisterMe
            // It uses the UserID from the cookie to link the Person to a Member.

            var memberResp = await client.PostAsync("api/Members/RegisterMe", null);

            if (!memberResp.IsSuccessStatusCode && memberResp.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                // Logic to handle failure (e.g. maybe they are already a member)
            }

            // 3. Done! Redirect to Dashboard
            return RedirectToAction("Index", "Home");
        }
    }
}