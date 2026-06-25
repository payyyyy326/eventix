using Eventix.Share.Category;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("Eventix");

            var categoryResult = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<CategoryResponse>>>(
                "api/category/categories");

            var eventFeatured = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<EventResponse>>>(
                "api/events?IsFeatured=true&CurrentPage=1&PageSize=5");

            var upcomingResult = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<EventResponse>>>(
                "api/events?SortBy=upcoming&CurrentPage=1&PageSize=5");

            var trendingResult = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<EventResponse>>>(
                "api/events?SortBy=view&CurrentPage=1&PageSize=8");

            var events = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<EventResponse>>>(
                "api/events");

            var model = new HomeViewModel
            {
                Categories = categoryResult?.Data?.DataList ?? new List<CategoryResponse>(),
                FeaturedEvents = eventFeatured?.Data?.DataList ?? new List<EventResponse>(),
                UpcomingEvents = upcomingResult?.Data?.DataList ?? new List<EventResponse>(),
                TrendingEvents = trendingResult?.Data?.DataList ?? new List<EventResponse>(),
                Events = events?.Data?.DataList ?? new List<EventResponse>(),
            };

            return View(model);
        }
    }
}