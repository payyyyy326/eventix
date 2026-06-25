using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

            var query = QueryHelpers.AddQueryString(
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

            var response = await client.GetAsync(query);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<EventResponse>>>();

            var model = new EventViewModel
            {
                Filter = request,
                Events = result?.Data ?? new PaginationResponse<EventResponse>()
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
    }
}