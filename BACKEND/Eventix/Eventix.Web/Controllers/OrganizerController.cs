using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.TicketType;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
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
        public IActionResult Dashboard()
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

            return View();
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
        public async Task<IActionResult> ManageEvent(Guid id)
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

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<TicketTypeResponse>>>(
                $"api/OrganizerProfile/events/{eventId}/ticket-types?CurrentPage=1&PageSize=20");

            return View(response?.Data ?? new PaginationResponse<TicketTypeResponse>());
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