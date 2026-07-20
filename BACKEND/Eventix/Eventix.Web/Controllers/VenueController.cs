using Eventix.Share.Common.Models;
using Eventix.Share.Venue;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Web.Controllers
{
    public class VenueController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VenueController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<VenueResponse>>>(
                "api/Venue/venues");

            var venues = response?.Data?.DataList ?? new List<VenueResponse>();

            return View(venues);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<VenueResponse>>(
                $"api/Venue/{id}");

            if (response == null || response.Data == null)
                return RedirectToAction("Index");

            return View(response.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            return View(new CreateVenueRequest());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateVenueRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                "api/Venue/create",
                model);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<VenueResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Cannot create venue.");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<VenueResponse>>(
                $"api/Venue/{id}");

            if (response == null || response.Data == null)
                return RedirectToAction("Index");

            var venue = response.Data;

            var model = new UpdateVenueRequest
            {
                Name = venue.Name,
                Address = venue.Address,
                City = venue.City,
                Capacity = venue.Capacity
            };

            ViewBag.VenueId = venue.Id;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, UpdateVenueRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.VenueId = id;
                return View(model);
            }

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync(
                $"api/Venue/{id}",
                model);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<VenueResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.VenueId = id;
                ModelState.AddModelError("", result?.Message ?? "Cannot update venue.");
                return View(model);
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult SeatMap(Guid id)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            ViewBag.VenueId = id;
            return View();
        }
    }
}
