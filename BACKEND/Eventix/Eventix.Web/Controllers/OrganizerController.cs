using Eventix.Share.Category;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;
using Eventix.Share.SeatMap;
using Eventix.Share.TicketType;
using Eventix.Share.Venue;
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
        public async Task<IActionResult> Events(
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int currentPage = 1,
            int pageSize = 10)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var httpResponse = await client.GetAsync(
                "api/OrganizerProfile/events?CurrentPage=1&PageSize=500");
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearExpiredSession();
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Auth");
            }

            var response = httpResponse.IsSuccessStatusCode
                ? await httpResponse.Content.ReadFromJsonAsync<
                    ApiResponseModel<PaginationResponse<OrganizerEventResponse>>>()
                : null;
            var allEvents = response?.Data?.DataList ?? [];            var filteredEvents = allEvents.AsEnumerable();
            search = search?.Trim();
            status = status?.Trim();

            if (!string.IsNullOrWhiteSpace(search))
                filteredEvents = filteredEvents.Where(item =>
                    item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.VenueName.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                filteredEvents = filteredEvents.Where(item =>
                    string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase));
            if (fromDate.HasValue)
                filteredEvents = filteredEvents.Where(item => item.StartTime.Date >= fromDate.Value.Date);
            if (toDate.HasValue)
                filteredEvents = filteredEvents.Where(item => item.StartTime.Date <= toDate.Value.Date);

            pageSize = new[] { 5, 10, 20, 50 }.Contains(pageSize) ? pageSize : 10;
            var orderedEvents = filteredEvents.OrderByDescending(item => item.CreatedAt).ToList();
            var totalRows = orderedEvents.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
            currentPage = Math.Clamp(currentPage, 1, totalPages);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TotalEventCount = allEvents.Count;
            ViewBag.StatusOptions = allEvents.Select(item => item.Status)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            return View(new PaginationResponse<OrganizerEventResponse>
            {
                DataList = orderedEvents.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
                TotalRows = totalRows,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize
            });
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
        public async Task<IActionResult> EditEvent(Guid id)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var ownerResponse = await client.GetAsync($"api/OrganizerProfile/events/{id}");
            if (ownerResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearExpiredSession();
                return RedirectToAction("Login", "Auth");
            }
            if (!ownerResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy sự kiện hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToAction(nameof(Events));
            }

            var detailResponse = await client.GetFromJsonAsync<
                ApiResponseModel<EventDetailResponse>>($"api/Events/{id}");
            if (detailResponse?.Data == null)
            {
                TempData["Error"] = "Không thể tải thông tin sự kiện.";
                return RedirectToAction(nameof(Events));
            }

            var detail = detailResponse.Data;
            var model = new EditEventViewModel
            {
                Id = detail.Id,
                Title = detail.Title,
                Slug = detail.Slug,
                CategoryId = detail.CategoryId,
                VenueId = detail.VenueId,
                Summary = detail.Summary,
                Description = detail.Description,
                ImageUrl = detail.ImageUrl,
                BannerUrl = detail.BannerUrl,
                StartTime = detail.StartTime,
                EndTime = detail.EndTime,
                Status = detail.Status,
                IsFeatured = detail.IsFeatured,
                PublishedAt = detail.PublishedAt
            };
            await PopulateEditEventOptionsAsync(client, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EditEventViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError(nameof(model.EndTime), "Thời gian kết thúc phải sau thời gian bắt đầu.");

            if (!ModelState.IsValid)
            {
                await PopulateEditEventOptionsAsync(client, model);
                return View(model);
            }

            var request = new UpdateEventRequest
            {
                CategoryId = model.CategoryId,
                VenueId = model.VenueId,
                Title = model.Title.Trim(),
                Slug = model.Slug,
                Description = model.Description,
                Summary = model.Summary,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Status = model.Status,
                IsFeatured = model.IsFeatured,
                PublishedAt = model.PublishedAt
            };

            var response = await client.PutAsJsonAsync($"api/Events/{model.Id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearExpiredSession();
                return RedirectToAction("Login", "Auth");
            }

            ApiResponseModel<EventDetailResponse>? result = null;
            try
            {
                result = await response.Content.ReadFromJsonAsync<
                    ApiResponseModel<EventDetailResponse>>();
            }
            catch (JsonException)
            {
                // The status code below still provides a useful fallback message.
            }

            if (!response.IsSuccessStatusCode || result?.IsSuccess != true)
            {
                ModelState.AddModelError(string.Empty,
                    result?.Message ?? "Không thể cập nhật sự kiện. Vui lòng kiểm tra thời gian và địa điểm.");
                await PopulateEditEventOptionsAsync(client, model);
                return View(model);
            }

            TempData["Success"] = "Cập nhật sự kiện thành công.";
            return RedirectToAction(nameof(ManageEvent), new { id = model.Id });
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
        private static async Task PopulateEditEventOptionsAsync(
            HttpClient client,
            EditEventViewModel model)
        {
            var categoryResponse = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<CategoryResponse>>>(
                "api/category/categories");
            var venueResponse = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<VenueResponse>>>(
                "api/Venue/venues?CurrentPage=1&PageSize=500");

            model.Categories = categoryResponse?.Data?.DataList ?? [];
            model.Venues = venueResponse?.Data?.DataList ?? [];
        }
        private void ClearExpiredSession()
        {
            Response.Cookies.Delete(CookieNames.Token);
            Response.Cookies.Delete(CookieNames.RefreshToken);
            Response.Cookies.Delete(CookieNames.UserName);
            Response.Cookies.Delete(CookieNames.AvatarUrl);
            Response.Cookies.Delete(CookieNames.Roles);
        }
    }
}