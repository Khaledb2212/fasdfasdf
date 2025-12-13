using Microsoft.AspNetCore.Mvc;

namespace Web_Project.Services
{
    public class PeopleMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PeopleMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("WebApi");

            var response = await client.GetAsync("api/People/GetPeople");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return Redirect("/Identity/Account/Login");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }

}
