using Eventix.Share.Common.Models;
using Eventix.Share.Event;
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
                    ["CurrentPage"] = request.CurrentPage.ToString(),
                    ["PageSize"] = request.PageSize.ToString()
                });

            var response = await client.GetAsync(query);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<EventResponse>>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.Error = result?.Message ?? "Cannot load events";
                return View(new PaginationResponse<EventResponse>());
            }

            return View(result.Data);
        }
    }
}