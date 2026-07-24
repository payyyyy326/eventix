using Eventix.Share.Booking;
using Eventix.Share.Category;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;

namespace Eventix.Web.Controllers
{
    public class EventController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(FilterEventRequest request)
        {
            request.CurrentPage = request.CurrentPage <= 0 ? 1 : request.CurrentPage;
            request.PageSize = request.PageSize <= 0 ? 12 : request.PageSize;

            var client = _httpClientFactory.CreateClient("Eventix");

            var eventQuery = QueryHelpers.AddQueryString(
                "api/events",
                new Dictionary<string, string?>
                {
                    ["Search"] = request.Search,
                    ["CategoryId"] = request.CategoryId?.ToString(),
                    ["VenueId"] = request.VenueId?.ToString(),
                    ["FromDate"] = request.FromDate?.ToString("yyyy-MM-dd"),
                    ["ToDate"] = request.ToDate?.ToString("yyyy-MM-dd"),
                    ["MinPrice"] = request.MinPrice?.ToString(),
                    ["MaxPrice"] = request.MaxPrice?.ToString(),
                    ["Status"] = request.Status,
                    ["SortBy"] = request.SortBy,
                    ["CurrentPage"] = request.CurrentPage.ToString(),
                    ["PageSize"] = request.PageSize.ToString()
                });

            var categoryQuery = QueryHelpers.AddQueryString(
                "api/category/categories",
                new Dictionary<string, string?>
                {
                    ["CurrentPage"] = "1",
                    ["PageSize"] = "100"
                });

            var eventTask = client.GetAsync(eventQuery);
            var categoryTask = client.GetAsync(categoryQuery);

            await Task.WhenAll(eventTask, categoryTask);

            var eventResponse = await eventTask;
            var categoryResponse = await categoryTask;

            var eventResult = await eventResponse.Content
                .ReadFromJsonAsync<
                    ApiResponseModel<PaginationResponse<EventResponse>>>();

            var categoryResult = await categoryResponse.Content
                .ReadFromJsonAsync<
                    ApiResponseModel<PaginationResponse<CategoryResponse>>>();

            var model = new EventViewModel
            {
                Filter = request,

                Events = eventResult?.Data
                    ?? new PaginationResponse<EventResponse>(),

                Categories = categoryResult?.Data?.DataList?.ToList()
                    ?? []
            };

            return View(model);
        }
        public async Task<IActionResult> Details(Guid id)
        {
            var client = _httpClientFactory.CreateClient("Eventix");

            var result = await client.GetFromJsonAsync<
                ApiResponseModel<EventDetailResponse>>(
                $"api/events/{id}");

            if (result == null || !result.IsSuccess || result.Data == null)
            {
                return NotFound();
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Booking(Guid id, Guid? ticketTypeId)
        {
            var token = Request.Cookies[SystemConstants.CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var eventData = await LoadBookingEventAsync(id);
            if (eventData == null)
                return NotFound();

            return View(new BookingViewModel
            {
                Event = eventData,
                Request = new CreateBookingRequest
                {
                    EventId = id,
                    TicketTypeId = ticketTypeId ?? Guid.Empty,
                    Quantity = 1
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(CreateBookingRequest request)
        {
            var token = Request.Cookies[SystemConstants.CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return await ReturnBookingViewAsync(request);

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/bookings", request);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<List<BookingResponse>>>();

            if (!response.IsSuccessStatusCode ||
                result == null ||
                !result.IsSuccess ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result?.Message ?? "Unable to reserve tickets.");
                return await ReturnBookingViewAsync(request);
            }

            return View("BookingConfirmation", result.Data);
        }

        private async Task<IActionResult> ReturnBookingViewAsync(
            CreateBookingRequest request)
        {
            var eventData = await LoadBookingEventAsync(request.EventId);
            if (eventData == null)
                return NotFound();

            return View("Booking", new BookingViewModel
            {
                Event = eventData,
                Request = request
            });
        }

        private async Task<EventBookingResponse?> LoadBookingEventAsync(Guid id)
        {
            var client = _httpClientFactory.CreateClient("Eventix");
            var result = await client.GetFromJsonAsync<
                ApiResponseModel<EventBookingResponse>>(
                $"api/events/{id}/booking");

            return result?.IsSuccess == true ? result.Data : null;
        }
    }
}
