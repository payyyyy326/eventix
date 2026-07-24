using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;
using Eventix.Share.SeatMap;
using Eventix.Share.TicketType;
using Eventix.Web.Models;
using Eventix.Web.Models.Organizer;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Web.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrganizerController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var token = Request.Cookies[CookieNames.Token];
            var roles = Request.Cookies[CookieNames.Roles] ?? "";

            var isOrganizer = roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(r => r.Equals("Organizer", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            if (!isOrganizer)
                return RedirectToAction("Index", "Home");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var all = new List<OrganizerEventResponse>();
            try
            {
                var httpResponse = await client.GetAsync(
                    "api/OrganizerProfile/events?CurrentPage=1&PageSize=200");

                if (httpResponse.IsSuccessStatusCode)
                {
                    var response = await httpResponse.Content
                        .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<OrganizerEventResponse>>>();
                    all = response?.Data?.DataList ?? new List<OrganizerEventResponse>();
                }
            }
            catch { /* API không trả về — dùng list rỗng, render dashboard trống */ }
            var now = DateTime.UtcNow;

            // Tự động tính range tháng dựa trên StartTime thực tế của events
            DateTime monthStart, monthEnd;
            if (all.Any())
            {
                var minEvent = all.Min(e => e.StartTime);
                var maxEvent = all.Max(e => e.StartTime);
                monthStart = new DateTime(minEvent.Year, minEvent.Month, 1);
                monthEnd   = new DateTime(maxEvent.Year, maxEvent.Month, 1);
            }
            else
            {
                monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
                monthEnd   = new DateTime(now.Year, now.Month, 1);
            }

            // Giới hạn tối đa 18 tháng để chart không quá dài
            if ((monthEnd - monthStart).TotalDays > 548)
                monthStart = monthEnd.AddMonths(-17);

            var totalMonths = ((monthEnd.Year - monthStart.Year) * 12)
                            + monthEnd.Month - monthStart.Month + 1;

            var months = Enumerable.Range(0, totalMonths)
                .Select(i => monthStart.AddMonths(i))
                .ToList();

            var revenueByMonth = months.Select(m =>
                all.Where(e => e.StartTime.Year == m.Year && e.StartTime.Month == m.Month)
                   .Sum(e => e.TotalRevenue)).ToList();

            var ticketsByMonth = months.Select(m =>
                all.Where(e => e.StartTime.Year == m.Year && e.StartTime.Month == m.Month)
                   .Sum(e => e.TotalTicketsSold)).ToList();

            var vm = new DashboardViewModel
            {
                TotalEvents      = all.Count,
                UpcomingEvents   = all.Count(e => e.StartTime > now),
                TotalTicketsSold = all.Sum(e => e.TotalTicketsSold),
                TotalRevenue     = all.Sum(e => e.TotalRevenue),

                MonthLabels      = months.Select(m => m.ToString("MM/yyyy")).ToList(),
                RevenueByMonth   = revenueByMonth,
                TicketsByMonth   = ticketsByMonth,

                EventsByStatus   = all
                    .GroupBy(e => e.Status ?? "Draft")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Events(PaginationRequest<OrganizerEventResponse> request)
        {
            request.CurrentPage = request.CurrentPage <= 0 ? 1 : request.CurrentPage;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var query =
                $"api/OrganizerProfile/events?CurrentPage={request.CurrentPage}&PageSize={request.PageSize}";

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<OrganizerEventResponse>>>(query);

            return View(response!.Data);
        }

        [HttpGet]
        public async Task<IActionResult> ManageEvent(Guid id, string tab = "general")
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<OrganizerEventDetailResponse>>(
                $"api/OrganizerProfile/events/{id}");

            if (response == null || !response.IsSuccess || response.Data == null)
                return RedirectToAction("Events");

            ViewBag.ManageTab = tab;

            // Tab map → load seat map layouts
            if (tab == "map")
            {
                var venueId = response.Data.VenueId;
                try
                {
                    var mapResp = await client.GetAsync($"api/Venue/{venueId}/seat-map");
                    if (mapResp.IsSuccessStatusCode)
                    {
                        var mapResult = await mapResp.Content
                            .ReadFromJsonAsync<ApiResponseModel<List<VenueSectionLayoutResponse>>>(
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ViewBag.SeatMapLayouts = mapResult?.Data ?? new List<VenueSectionLayoutResponse>();
                    }
                    else
                    {
                        ViewBag.SeatMapLayouts = new List<VenueSectionLayoutResponse>();
                    }
                }
                catch
                {
                    ViewBag.SeatMapLayouts = new List<VenueSectionLayoutResponse>();
                }
            }

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> TicketTypes(Guid eventId)
        {
            ViewBag.EventId = eventId;
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Lấy thông tin event để biết status
            var eventResponse = await client.GetFromJsonAsync<
                ApiResponseModel<OrganizerEventDetailResponse>>(
                $"api/OrganizerProfile/events/{eventId}");

            ViewBag.EventStatus = eventResponse?.Data?.Status ?? "Draft";
            ViewBag.EventTitle  = eventResponse?.Data?.Title ?? "";
            ViewBag.EventImage  = eventResponse?.Data?.BannerUrl ?? eventResponse?.Data?.ImageUrl ?? "";

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<TicketTypeResponse>>>(
                $"api/OrganizerProfile/events/{eventId}/ticket-types?CurrentPage=1&PageSize=50");

            return View(response?.Data ?? new PaginationResponse<TicketTypeResponse>());
        }


        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                "api/OrganizerProfile/organizer-detail");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải hồ sơ tổ chức. Vui lòng thử lại.";
                return RedirectToAction("Dashboard");
            }

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<OrganizerProfileResponse>>();

            if (result == null || !result.IsSuccess || result.Data == null)
            {
                TempData["Error"] = result?.Message ?? "Không thể tải hồ sơ tổ chức.";
                return RedirectToAction("Dashboard");
            }

            var profile = result.Data;

            var model = new OrganizerProfileViewModel
            {
                Id = profile.Id,
                OrganizationName = profile.OrganizationName,
                Description = profile.Description,
                ContactEmail = profile.ContactEmail,
                ContactPhone = profile.ContactPhone,
                Status = profile.Status,
                ApprovedBy = profile.ApprovedBy,
                ApprovedAt = profile.ApprovedAt,
                CreatedAt = profile.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
    OrganizerProfileViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var request = new UpdateOrganizerProfileRequest
            {
                OrganizationName = model.OrganizationName,
                Description = model.Description,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone
            };

            var response = await client.PutAsJsonAsync(
                "api/OrganizerProfile/detail",
                request);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<OrganizerProfileResponse>>();

            if (!response.IsSuccessStatusCode ||
                result == null ||
                !result.IsSuccess ||
                result.Data == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result?.Message ?? "Cannot update organizer profile.");

                return View(model);
            }

            TempData["Success"] =
                "Organizer profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }
    }
}