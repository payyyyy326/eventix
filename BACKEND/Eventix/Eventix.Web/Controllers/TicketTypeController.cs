using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.TicketType;
using Eventix.Share.VenueZone;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Web.Controllers
{
    public class TicketTypeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TicketTypeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid eventId)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Lấy thông tin event để có VenueId
            var eventResponse = await client.GetFromJsonAsync<
                ApiResponseModel<OrganizerEventDetailResponse>>(
                $"api/OrganizerProfile/events/{eventId}");

            if (eventResponse?.Data == null)
                return RedirectToAction("Events", "Organizer");

            var venueId = eventResponse.Data.VenueId;

            ViewBag.EventId = eventId;
            ViewBag.Zones = await LoadZonesAsync(client, venueId);

            // Load zone available capacity cho event này (slots còn trống)
            var capacityResponse = await client.GetFromJsonAsync<
                ApiResponseModel<List<ZoneAvailableCapacityResponse>>>(
                $"api/VenueZone/event/{eventId}/zone-capacity");

            ViewBag.ZoneCapacity = capacityResponse?.Data ?? new List<ZoneAvailableCapacityResponse>();

            return View(new CreateTicketTypeRequest());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid eventId, CreateTicketTypeRequest model)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (!ModelState.IsValid)
            {
                var eventResp = await client.GetFromJsonAsync<
                    ApiResponseModel<OrganizerEventDetailResponse>>(
                    $"api/OrganizerProfile/events/{eventId}");

                ViewBag.EventId = eventId;
                ViewBag.Zones = await LoadZonesAsync(client, eventResp?.Data?.VenueId ?? Guid.Empty);

                var capResp = await client.GetFromJsonAsync<
                    ApiResponseModel<List<ZoneAvailableCapacityResponse>>>(
                    $"api/VenueZone/event/{eventId}/zone-capacity");
                ViewBag.ZoneCapacity = capResp?.Data ?? new List<ZoneAvailableCapacityResponse>();

                return View(model);
            }

            var response = await client.PostAsJsonAsync(
                $"api/OrganizerProfile/events/{eventId}/ticket-types",
                model);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                var eventResp = await client.GetFromJsonAsync<
                    ApiResponseModel<OrganizerEventDetailResponse>>(
                    $"api/OrganizerProfile/events/{eventId}");

                ViewBag.EventId = eventId;
                ViewBag.Zones = await LoadZonesAsync(client, eventResp?.Data?.VenueId ?? Guid.Empty);

                var capResp = await client.GetFromJsonAsync<
                    ApiResponseModel<List<ZoneAvailableCapacityResponse>>>(
                    $"api/VenueZone/event/{eventId}/zone-capacity");
                ViewBag.ZoneCapacity = capResp?.Data ?? new List<ZoneAvailableCapacityResponse>();

                ModelState.AddModelError("", result?.Message ?? "Cannot create ticket type.");
                return View(model);
            }

            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
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
                ApiResponseModel<TicketTypeResponse>>(
                $"api/OrganizerProfile/ticket-types/{id}");

            if (response?.Data == null)
                return RedirectToAction("Events", "Organizer");

            var ticket = response.Data;

            var model = new UpdateTicketTypeRequest
            {
                Name        = ticket.Name,
                Description = ticket.Description,
                Price       = ticket.Price,
                Quantity    = ticket.Quantity,
                SaleStartTime = ticket.SaleStartTime,
                SaleEndTime   = ticket.SaleEndTime
            };

            ViewBag.TicketTypeId  = ticket.Id;
            ViewBag.EventId       = ticket.EventId;
            ViewBag.ZoneName      = ticket.ZoneName ?? ticket.Section ?? "N/A";
            ViewBag.HasSeats      = ticket.HasSeats;
            ViewBag.IsSeatRequired = ticket.IsSeatRequired;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, Guid eventId, UpdateTicketTypeRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TicketTypeId = id;
                ViewBag.EventId      = eventId;
                return View(model);
            }

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync(
                $"api/OrganizerProfile/ticket-types/{id}",
                model);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.TicketTypeId = id;
                ViewBag.EventId      = eventId;
                ModelState.AddModelError("", result?.Message ?? "Cannot update ticket type.");
                return View(model);
            }

            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(Guid id, Guid eventId)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PatchAsync(
                $"api/OrganizerProfile/ticket-types/{id}/deactivate",
                null);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
                TempData["Error"]   = result?.Message ?? "Cannot deactivate ticket type.";
            else
                TempData["Success"] = result.Message ?? "Ticket type deactivated successfully.";

            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        private async Task<List<VenueZoneResponse>> LoadZonesAsync(HttpClient client, Guid venueId)
        {
            if (venueId == Guid.Empty)
                return new List<VenueZoneResponse>();

            var response = await client.GetAsync($"api/VenueZone/venue/{venueId}");

            if (!response.IsSuccessStatusCode)
                return new List<VenueZoneResponse>();

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<List<VenueZoneResponse>>>();

            return result?.Data ?? new List<VenueZoneResponse>();
        }
    }
}