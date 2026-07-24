using Eventix.Share.Common.Models;
using Eventix.Share.TicketType;
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

        // GET: TicketType/Create?eventId=...
        [HttpGet]
        public IActionResult Create(Guid eventId)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            ViewBag.EventId = eventId;
            return View(new CreateTicketTypeRequest());
        }

        // POST: TicketType/Create?eventId=...
        [HttpPost]
        public async Task<IActionResult> Create(Guid eventId, CreateTicketTypeRequest model)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.EventId = eventId;
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                $"api/TicketType/event/{eventId}",
                model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.EventId = eventId;
                ModelState.AddModelError("", result?.Message ?? "Cannot create ticket type.");
                return View(model);
            }

            TempData["Success"] = "Ticket type created successfully.";
            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
        }

        // GET: TicketType/Edit?id=...
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
                Name          = ticket.Name,
                Description   = ticket.Description,
                Price         = ticket.Price,
                Quantity      = ticket.Quantity,
                SaleStartTime = ticket.SaleStartTime,
                SaleEndTime   = ticket.SaleEndTime,
                SectionColor  = ticket.SectionColor
            };

            ViewBag.TicketTypeId   = ticket.Id;
            ViewBag.EventId        = ticket.EventId;
            ViewBag.Section        = ticket.Section ?? "N/A";
            ViewBag.IsSeatRequired = ticket.IsSeatRequired;

            return View(model);
        }

        // POST: TicketType/Edit?id=...&eventId=...
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

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.TicketTypeId = id;
                ViewBag.EventId      = eventId;
                ModelState.AddModelError("", result?.Message ?? "Cannot update ticket type.");
                return View(model);
            }

            TempData["Success"] = "Ticket type updated successfully.";
            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
        }

        // POST: TicketType/Deactivate
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
                TempData["Success"] = "Ticket type deactivated.";

            return RedirectToAction("TicketTypes", "Organizer", new { eventId });
        }
    }
}
