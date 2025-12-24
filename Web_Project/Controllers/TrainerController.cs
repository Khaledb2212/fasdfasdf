//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Security.Claims;
//using System.Text;
//using System.Text.Json;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Web_Project.Models;

//namespace Web_Project.Controllers
//{
//    [Authorize(Roles = "Trainer")]
//    public class TrainerController : Controller
//    {
//        private readonly IHttpClientFactory _httpClientFactory;

//        public TrainerController(IHttpClientFactory httpClientFactory)
//        {
//            _httpClientFactory = httpClientFactory;
//        }

//        [HttpGet]
//        public async Task<IActionResult> Index()
//        {
//            var vm = new TrainerPortalVm
//            {
//                DayOfWeek = (int)DateTime.Today.DayOfWeek
//            };

//            var client = _httpClientFactory.CreateClient("WebApi");

//            // -----------------------------
//            // 1) Load Services (Dropdown)
//            // -----------------------------
//            var servicesResp = await client.GetAsync("api/Services/GetServices");
//            if (servicesResp.StatusCode == HttpStatusCode.Unauthorized)
//                return Redirect("/Identity/Account/Login");

//            if (servicesResp.IsSuccessStatusCode)
//            {
//                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//                var services = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>(options) ?? new();

//                vm.Services = services.Select(s => new SelectListItem
//                {
//                    Value = s.ServiceID.ToString(),
//                    Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
//                }).ToList();
//            }
//            else
//            {
//                TempData["Error"] = "Could not load Services list from API.";
//            }

//            // -----------------------------
//            // 2) Load MySlots
//            // API: GET api/TrainerAvailabilities/MySlots
//            // Returns: availabilityId, dayOfWeek, startTime("HH:mm"), endTime("HH:mm"),
//            //          serviceTypeId, serviceName
//            // -----------------------------
//            var slotsResp = await client.GetAsync("api/TrainerAvailabilities/MySlots");
//            if (slotsResp.StatusCode == HttpStatusCode.Unauthorized)
//                return Redirect("/Identity/Account/Login");

//            if (slotsResp.IsSuccessStatusCode)
//            {
//                var rawSlots = await slotsResp.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();

//                vm.MySlots = rawSlots.Select(x => new MySlotRow
//                {
//                    AvailabilityId = x.TryGetProperty("availabilityId", out var v1) ? v1.GetInt32() :
//                                     (x.TryGetProperty("AvailabilityId", out var v1b) ? v1b.GetInt32() : 0),

//                    DayOfWeek = x.TryGetProperty("dayOfWeek", out var v2) ? v2.GetInt32() :
//                                (x.TryGetProperty("DayOfWeek", out var v2b) ? v2b.GetInt32() : 0),

//                    StartTime = x.TryGetProperty("startTime", out var v3) ? (v3.GetString() ?? "") :
//                               (x.TryGetProperty("StartTime", out var v3b) ? (v3b.GetString() ?? "") : ""),

//                    EndTime = x.TryGetProperty("endTime", out var v4) ? (v4.GetString() ?? "") :
//                             (x.TryGetProperty("EndTime", out var v4b) ? (v4b.GetString() ?? "") : ""),

//                    ServiceTypeId = x.TryGetProperty("serviceTypeId", out var v5) ? v5.GetInt32() :
//                                    (x.TryGetProperty("ServiceTypeId", out var v5b) ? v5b.GetInt32() : 0),

//                    ServiceName = x.TryGetProperty("serviceName", out var v6) ? v6.GetString() :
//                                  (x.TryGetProperty("ServiceName", out var v6b) ? v6b.GetString() : null),
//                })
//                .OrderBy(s => s.DayOfWeek)
//                .ThenBy(s => s.StartTime)
//                .ToList();
//            }
//            else
//            {
//                TempData["Error"] = "Could not load your Slots from API.";
//            }

//            // -----------------------------
//            // 3) Load Trainer Appointments (Upcoming)
//            // API: GET api/Appointments/MyTrainerAppointments?upcomingOnly=true
//            // Then we split:
//            // - PendingAppointments => IsApproved == false
//            // - UpcomingApprovedAppointments => IsApproved == true
//            // -----------------------------
//            var apptResp = await client.GetAsync("api/Appointments/MyTrainerAppointments?upcomingOnly=true");
//            if (apptResp.StatusCode == HttpStatusCode.Unauthorized)
//                return Redirect("/Identity/Account/Login");

//            if (apptResp.IsSuccessStatusCode)
//            {
//                var rawAppts = await apptResp.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();

//                var allUpcoming = rawAppts.Select(x =>
//                {
//                    DateTime ReadDt(string k1, string k2)
//                    {
//                        if (x.TryGetProperty(k1, out var a) || x.TryGetProperty(k2, out a))
//                        {
//                            if (a.ValueKind == JsonValueKind.String)
//                            {
//                                var s = a.GetString();
//                                if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
//                                    return dt;
//                            }
//                            if (a.ValueKind == JsonValueKind.Number)
//                            {
//                                // very rare; ignore
//                            }
//                        }
//                        return DateTime.MinValue;
//                    }

//                    bool ReadBool(string k1, string k2)
//                    {
//                        if (x.TryGetProperty(k1, out var b) || x.TryGetProperty(k2, out b))
//                        {
//                            if (b.ValueKind == JsonValueKind.True) return true;
//                            if (b.ValueKind == JsonValueKind.False) return false;
//                        }
//                        return false;
//                    }

//                    decimal ReadDecimal(string k1, string k2)
//                    {
//                        if (x.TryGetProperty(k1, out var d) || x.TryGetProperty(k2, out d))
//                        {
//                            if (d.ValueKind == JsonValueKind.Number) return d.GetDecimal();
//                            if (d.ValueKind == JsonValueKind.String && decimal.TryParse(d.GetString(), out var dd)) return dd;
//                        }
//                        return 0;
//                    }

//                    int ReadInt(string k1, string k2)
//                    {
//                        if (x.TryGetProperty(k1, out var i) || x.TryGetProperty(k2, out i))
//                            return i.GetInt32();
//                        return 0;
//                    }

//                    string ReadStr(string k1, string k2, string fallback)
//                    {
//                        if (x.TryGetProperty(k1, out var s) || x.TryGetProperty(k2, out s))
//                            return s.GetString() ?? fallback;
//                        return fallback;
//                    }

//                    return new PendingAppointmentRow
//                    {
//                        AppointmentID = ReadInt("appointmentID", "AppointmentID"),
//                        StartTime = ReadDt("startTime", "StartTime"),
//                        EndTime = ReadDt("endTime", "EndTime"),
//                        IsApproved = ReadBool("isApproved", "IsApproved"),
//                        Fee = ReadDecimal("fee", "Fee"),
//                        MemberName = ReadStr("memberName", "MemberName", "Unknown"),
//                        ServiceName = ReadStr("serviceName", "ServiceName", "Unknown"),
//                    };
//                })
//                .Where(a => a.StartTime != DateTime.MinValue)
//                .OrderBy(a => a.StartTime)
//                .ToList();

//                vm.PendingAppointments = allUpcoming
//                    .Where(a => a.IsApproved == false)
//                    .ToList();

//                vm.UpcomingApprovedAppointments = allUpcoming
//                    .Where(a => a.IsApproved == true)
//                    .ToList();
//            }
//            else
//            {
//                TempData["Error"] = "Could not load your upcoming appointments from API.";
//            }

//            return View(vm);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> AddMySlot(TrainerPortalVm vm)
//        {
//            // Basic server-side validation for the form part
//            if (vm.ServiceTypeId <= 0)
//                ModelState.AddModelError("", "Service is required.");

//            if (vm.StartTime >= vm.EndTime)
//                ModelState.AddModelError("", "StartTime must be before EndTime.");

//            if (!ModelState.IsValid)
//                return await Index(); // reload portal data + show errors

//            var client = _httpClientFactory.CreateClient("WebApi");

//            // API DTO expects DateTime, but it only uses TimeOfDay.
//            // So we anchor to a fixed date for consistency.
//            var baseDate = new DateTime(2000, 1, 1);
//            var startDt = baseDate.Add(vm.StartTime);
//            var endDt = baseDate.Add(vm.EndTime);

//            var payload = new
//            {
//                DayOfWeek = vm.DayOfWeek,
//                StartTime = startDt,
//                EndTime = endDt,
//                ServiceTypeId = vm.ServiceTypeId
//            };

//            var resp = await client.PostAsJsonAsync("api/TrainerAvailabilities/AddMySlot", payload);

//            if (resp.StatusCode == HttpStatusCode.Unauthorized)
//                return Redirect("/Identity/Account/Login");

//            if (resp.StatusCode == HttpStatusCode.Forbidden)
//            {
//                TempData["Error"] = "Forbidden: You must be logged in as Trainer.";
//                return RedirectToAction(nameof(Index));
//            }

//            if (resp.IsSuccessStatusCode)
//            {
//                TempData["Success"] = "Slot added successfully.";
//                return RedirectToAction(nameof(Index));
//            }

//            var body = await resp.Content.ReadAsStringAsync();
//            TempData["Error"] = $"Could not add slot: {body}";
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ApproveAppointment(int appointmentId)
//        {
//            if (appointmentId <= 0)
//            {
//                TempData["Error"] = "Invalid appointment id.";
//                return RedirectToAction(nameof(Index));
//            }

//            var client = _httpClientFactory.CreateClient("WebApi");

//            // Your API: [HttpPut("Approve")] => expects ?id=
//            var resp = await client.PutAsync(
//                $"api/Appointments/Approve?id={appointmentId}",
//                new StringContent("", Encoding.UTF8, "application/json")
//            );

//            if (resp.StatusCode == HttpStatusCode.Unauthorized)
//                return Redirect("/Identity/Account/Login");

//            if (resp.StatusCode == HttpStatusCode.Forbidden)
//            {
//                TempData["Error"] = "Forbidden: You can only approve your own appointments.";
//                return RedirectToAction(nameof(Index));
//            }

//            if (resp.IsSuccessStatusCode)
//            {
//                TempData["Success"] = "Appointment approved.";
//                return RedirectToAction(nameof(Index));
//            }

//            var body = await resp.Content.ReadAsStringAsync();
//            TempData["Error"] = $"Could not approve appointment: {body}";
//            return RedirectToAction(nameof(Index));
//        }
//        // ----------------------------
//        // Helper: forward the logged-in user's cookie to the API
//        // ----------------------------
//        private HttpClient CreateApiClientWithCookie()
//        {
//            var client = _httpClientFactory.CreateClient("WebApi");

//            // Forward browser cookies to API so [Authorize] works there
//            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
//            {
//                client.DefaultRequestHeaders.Remove("Cookie");
//                client.DefaultRequestHeaders.Add("Cookie", cookieHeader.ToString());
//            }

//            return client;
//        }



//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Text.Json;
using Web_Project.Models;

namespace Web_Project.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TrainerController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

 
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = new TrainerPortalVm();
            await LoadPortalData(vm);
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSlot(TrainerPortalVm vm)
        {
            // Validate only the "Add slot form" fields
            if (!ModelState.IsValid)
            {
                await LoadPortalData(vm);
                return View("Index", vm);
            }

            // Convert TimeSpan -> DateTime payload that matches your API AddAvailabilityDto
            // (Your API reads TimeOfDay, so date does not matter)
            var anchor = new DateTime(2000, 1, 1);
            var startDt = anchor.Add(vm.StartTime);
            var endDt = anchor.Add(vm.EndTime);

            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.PostAsJsonAsync("api/TrainerAvailabilities/AddMySlot", new
            {
                dayOfWeek = vm.DayOfWeek,
                startTime = startDt,
                endTime = endDt,
                serviceTypeId = vm.ServiceTypeId
            });

            if (resp.IsSuccessStatusCode)
                TempData["Success"] = "Slot added successfully.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSlot(int availabilityId)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.DeleteAsync($"api/TrainerAvailabilities/DeleteTrainerAvailability?id={availabilityId}");

            if (resp.IsSuccessStatusCode)
                TempData["Success"] = "Slot removed.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int appointmentId)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var resp = await client.PutAsync($"api/Appointments/Approve?id={appointmentId}", null);

            if (resp.IsSuccessStatusCode)
            {
                TempData["Success"] = "Appointment approved.";
                return RedirectToAction(nameof(Index));
            }

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return Redirect("/Identity/Account/Login");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "Forbidden: you can only approve your own appointments.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = await resp.Content.ReadAsStringAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string expertiseAreas, string? description)
        {
            if (string.IsNullOrWhiteSpace(expertiseAreas))
            {
                TempData["Error"] = "Expertise areas is required.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("WebApi");

            // This requires your API endpoint:
            // PUT api/Trainers/UpdateMyProfile  (CreateTrainerDto payload)
            var resp = await client.PutAsJsonAsync("api/Trainers/UpdateMyProfile", new
            {
                expertiseAreas = expertiseAreas,
                description = description
            });

            if (resp.IsSuccessStatusCode)
                TempData["Success"] = "Profile updated.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(Index));
        }

        //private async Task LoadPortalData(TrainerPortalVm vm)
        //{
        //    var client = _httpClientFactory.CreateClient("WebApi");
        //    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //    vm.Services = new List<SelectListItem>();
        //    vm.MySlots = new List<MySlotRow>();
        //    vm.PendingAppointments = new List<PendingAppointmentRow>();
        //    vm.UpcomingApprovedAppointments = new List<PendingAppointmentRow>();

        //    var mySkillIds = new List<int>();

        //    var myResp = await client.GetAsync("api/Trainers/MyProfile");
        //    if (!myResp.IsSuccessStatusCode)
        //    {
        //        TempData["Error"] = "Could not load your trainer profile: " + await myResp.Content.ReadAsStringAsync();
        //        return;
        //    }

        //    var myProfile = await myResp.Content.ReadFromJsonAsync<ApiTrainer>(options);
        //    if (myProfile == null)
        //    {
        //        TempData["Error"] = "Could not read trainer profile (null).";
        //        return;
        //    }

        //    vm.ExpertiseAreas = myProfile.ExpertiseAreas;
        //    vm.Description = myProfile.Description;

        //    if (myProfile.Skills != null)
        //        mySkillIds = myProfile.Skills.Select(s => s.ServiceID).ToList();

        //    var servicesResp = await client.GetAsync("api/Services/GetServices");
        //    if (servicesResp.IsSuccessStatusCode)
        //    {
        //        var allServices = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>(options) ?? new();

        //        var myServices = allServices
        //            .Where(s => mySkillIds.Contains(s.ServiceID))
        //            .ToList();

        //        vm.Services = myServices.Select(s => new SelectListItem
        //        {
        //            Value = s.ServiceID.ToString(),
        //            Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
        //        }).ToList();

        //        if (vm.Services.Count == 0)
        //        {
        //            TempData["Error"] = "No skills found for your trainer profile. Ask Admin to assign skills first.";
        //        }
        //    }
        //    else
        //    {
        //        TempData["Error"] = "Could not load Services from API.";
        //    }

        //    var slotsResp = await client.GetAsync("api/TrainerAvailabilities/MySlots");
        //    if (slotsResp.IsSuccessStatusCode)
        //    {
        //        var json = await slotsResp.Content.ReadAsStringAsync();
        //        using var doc = JsonDocument.Parse(json);

        //        foreach (var el in doc.RootElement.EnumerateArray())
        //        {
        //            vm.MySlots.Add(new MySlotRow
        //            {
        //                AvailabilityId = GetInt(el, "availabilityId"),
        //                DayOfWeek = GetInt(el, "dayOfWeek"),
        //                StartTime = GetString(el, "startTime") ?? "00:00",
        //                EndTime = GetString(el, "endTime") ?? "00:00",
        //                ServiceTypeId = GetInt(el, "serviceTypeId"),
        //                ServiceName = GetString(el, "serviceName")
        //            });
        //        }
        //    }

        //    vm.PendingAppointments = await LoadTrainerAppointments(client, pendingOnly: true, upcomingOnly: false);

        //    var upcoming = await LoadTrainerAppointments(client, pendingOnly: false, upcomingOnly: true);

        //    vm.UpcomingApprovedAppointments = upcoming
        //        .Where(a => a.IsApproved)
        //        .OrderBy(a => a.StartTime)
        //        .ToList();
        //}


        private async Task LoadPortalData(TrainerPortalVm vm)
        {
            var client = _httpClientFactory.CreateClient("WebApi");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // -----------------------------
            // A) Load my profile text (Expertise/Description)
            // -----------------------------
            var trainersResp = await client.GetAsync("api/Trainers/GetTrainers");
            if (trainersResp.IsSuccessStatusCode)
            {
                var allTrainers = await trainersResp.Content.ReadFromJsonAsync<List<ApiTrainer>>(options) ?? new();

                // Match by logged-in email (Identity username)
                var myProfile = allTrainers.FirstOrDefault(t =>
                    string.Equals(t.Person?.Email, User.Identity?.Name, StringComparison.OrdinalIgnoreCase));

                if (myProfile != null)
                {
                    vm.ExpertiseAreas = myProfile.ExpertiseAreas;
                    vm.Description = myProfile.Description;
                }
            }

            // -----------------------------
            // B) Load my skills (THE IMPORTANT FIX)
            // Use API: GET api/TrainerSkills/MySkills
            // -----------------------------
            var mySkillServiceIds = new List<int>();

            var mySkillsResp = await client.GetAsync("api/TrainerSkills/MySkills");
            if (mySkillsResp.IsSuccessStatusCode)
            {
                var json = await mySkillsResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    // expected: { id, serviceId, serviceName }
                    var sid = GetInt(el, "serviceId");
                    if (sid > 0) mySkillServiceIds.Add(sid);
                }
            }
            else
            {
                // If unauthorized, your cookie is not reaching the API
                TempData["Error"] = "Could not load your skills from API (MySkills).";
            }

            // -----------------------------
            // C) Load services dropdown (filtered by my skills)
            // -----------------------------
            vm.Services = new();

            var servicesResp = await client.GetAsync("api/Services/GetServices");
            if (servicesResp.IsSuccessStatusCode)
            {
                var allServices = await servicesResp.Content.ReadFromJsonAsync<List<ServiceDTO>>(options) ?? new();

                var myServices = allServices
                    .Where(s => mySkillServiceIds.Contains(s.ServiceID))
                    .ToList();

                if (myServices.Count == 0)
                {
                    TempData["Error"] = "No skills found for your trainer profile. Ask Admin to assign skills to you.";
                    // Keep dropdown empty on purpose (so trainer cannot add slot without skills)
                }
                else
                {
                    vm.Services = myServices.Select(s => new SelectListItem
                    {
                        Value = s.ServiceID.ToString(),
                        Text = $"{s.ServiceName} (${s.FeesPerHour}/hr)"
                    }).ToList();
                }
            }
            else
            {
                TempData["Error"] = "Could not load Services from API.";
            }

            // -----------------------------
            // D) Load my slots
            // -----------------------------
            vm.MySlots = new();
            var slotsResp = await client.GetAsync("api/TrainerAvailabilities/MySlots");
            if (slotsResp.IsSuccessStatusCode)
            {
                var json = await slotsResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    vm.MySlots.Add(new MySlotRow
                    {
                        AvailabilityId = GetInt(el, "availabilityId"),
                        DayOfWeek = GetInt(el, "dayOfWeek"),
                        StartTime = GetString(el, "startTime") ?? "00:00",
                        EndTime = GetString(el, "endTime") ?? "00:00",
                        ServiceTypeId = GetInt(el, "serviceTypeId"),
                        ServiceName = GetString(el, "serviceName")
                    });
                }
            }

            // -----------------------------
            // E) Load ALL trainer appointments (RAW), then split locally
            // -----------------------------
            var all = await LoadTrainerAppointmentsRaw(client);

            // Pending = not approved, today+
            vm.PendingAppointments = all
                .Where(a => !a.IsApproved && a.StartTime.Date >= DateTime.Today)
                .OrderBy(a => a.StartTime)
                .ToList();

            // Approved = approved, today+
            vm.UpcomingApprovedAppointments = all
                .Where(a => a.IsApproved && a.StartTime.Date >= DateTime.Today)
                .OrderBy(a => a.StartTime)
                .ToList();
        }

        private async Task<List<PendingAppointmentRow>> LoadTrainerAppointmentsRaw(HttpClient client)
        {
            var list = new List<PendingAppointmentRow>();

            // IMPORTANT: call WITHOUT pendingOnly/upcomingOnly
            var resp = await client.GetAsync("api/Appointments/MyTrainerAppointments");
            if (!resp.IsSuccessStatusCode) return list;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new PendingAppointmentRow
                {
                    AppointmentID = GetInt(el, "appointmentID"),
                    StartTime = GetDateTime(el, "startTime"),
                    EndTime = GetDateTime(el, "endTime"),
                    IsApproved = GetBool(el, "isApproved"),
                    Fee = GetDecimal(el, "fee"),
                    MemberName = GetString(el, "memberName") ?? "Unknown",
                    ServiceName = GetString(el, "serviceName") ?? "Unknown"
                });
            }

            return list;
        }


        private async Task<List<PendingAppointmentRow>> LoadTrainerAppointments(HttpClient client, bool pendingOnly, bool upcomingOnly)
        {
            var list = new List<PendingAppointmentRow>();

            var url = $"api/Appointments/MyTrainerAppointments?pendingOnly={pendingOnly.ToString().ToLower()}&upcomingOnly={upcomingOnly.ToString().ToLower()}";
            var resp = await client.GetAsync(url);

            if (!resp.IsSuccessStatusCode) return list;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new PendingAppointmentRow
                {
                    AppointmentID = GetInt(el, "appointmentID"),
                    StartTime = GetDateTime(el, "startTime"),
                    EndTime = GetDateTime(el, "endTime"),
                    IsApproved = GetBool(el, "isApproved"),
                    Fee = GetDecimal(el, "fee"),
                    MemberName = GetString(el, "memberName") ?? "Unknown",
                    ServiceName = GetString(el, "serviceName") ?? "Unknown"
                });
            }

            return list.OrderBy(a => a.StartTime).ToList();
        }


        private static string? GetString(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
            var alt = char.ToUpper(name[0]) + name.Substring(1);
            if (el.TryGetProperty(alt, out var v2) && v2.ValueKind == JsonValueKind.String) return v2.GetString();
            return null;
        }

        private static int GetInt(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            var alt = char.ToUpper(name[0]) + name.Substring(1);
            if (el.TryGetProperty(alt, out var v2) && v2.ValueKind == JsonValueKind.Number) return v2.GetInt32();
            return 0;
        }

        private static bool GetBool(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False) return v.GetBoolean();
            var alt = char.ToUpper(name[0]) + name.Substring(1);
            if (el.TryGetProperty(alt, out var v2) && v2.ValueKind is JsonValueKind.True or JsonValueKind.False) return v2.GetBoolean();
            return false;
        }

        private static DateTime GetDateTime(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), out var dt))
                return dt;

            var alt = char.ToUpper(name[0]) + name.Substring(1);
            if (el.TryGetProperty(alt, out var v2) && v2.ValueKind == JsonValueKind.String && DateTime.TryParse(v2.GetString(), out var dt2))
                return dt2;

            return DateTime.MinValue;
        }

        private static decimal GetDecimal(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
            var alt = char.ToUpper(name[0]) + name.Substring(1);
            if (el.TryGetProperty(alt, out var v2) && v2.ValueKind == JsonValueKind.Number) return v2.GetDecimal();
            return 0;
        }
    }
}

