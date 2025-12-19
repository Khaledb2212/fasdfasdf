using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Http.Json;
using Web_Project.Models;

namespace Web_Project.Controllers
{
    [Authorize]
    public class AccountSetupController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<UserDetails> _userManager;
        private readonly SignInManager<UserDetails> _signInManager;

        public AccountSetupController(
            IHttpClientFactory httpClientFactory,
            UserManager<UserDetails> userManager,
            SignInManager<UserDetails> signInManager)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // POST: /AccountSetup/BecomeMember
        [HttpPost]
        public async Task<IActionResult> BecomeMember()
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.PostAsync("api/Members/RegisterMe", null);

            // allow OK or "already a member" (409)
            if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return Content($"API error: {(int)resp.StatusCode} - {body}");
            }

            // Assign Identity role (this is what fixes 403)
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!await _userManager.IsInRoleAsync(user, "Member"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Member");
                if (!result.Succeeded)
                    return Content("Role assign failed: " + string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            // Refresh cookie so API immediately sees the new role
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", "Home");
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

            // allow OK or "already a trainer" (409)
            if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return Content($"API error: {(int)resp.StatusCode} - {body}");
            }

            // Assign Identity role (this is what fixes 403)
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!await _userManager.IsInRoleAsync(user, "Trainer"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Trainer");
                if (!result.Succeeded)
                    return Content("Role assign failed: " + string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            // Refresh cookie so API immediately sees the new role
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", "Home");
        }
    }
}
