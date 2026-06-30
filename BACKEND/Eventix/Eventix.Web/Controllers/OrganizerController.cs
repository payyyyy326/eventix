using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

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
        public IActionResult Dashboard()
        {
            var token = Request.Cookies["token"];
            var roles = Request.Cookies["roles"] ?? "";

            var isOrganizer = roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(r => r.Equals("Organizer", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            if (!isOrganizer)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Events(PaginationRequest<OrganizerEventResponse> request)
        {
            request.CurrentPage = request.CurrentPage <= 0 ? 1 : request.CurrentPage;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var token = Request.Cookies["token"];

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
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }
    }
}