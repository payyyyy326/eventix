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

        [HttpGet]
        public async Task<IActionResult> Create(Guid eventId)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            ViewBag.EventId = eventId;
            ViewBag.Sections = await LoadSectionsAsync(client, eventId);

            return View(new CreateTicketTypeRequest());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid eventId, CreateTicketTypeRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.EventId = eventId;
                return View(model);
            }

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                $"api/OrganizerProfile/events/{eventId}/ticket-types",
                model);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ViewBag.EventId = eventId;

                ViewBag.Sections = await LoadSectionsAsync(client, eventId);

                ModelState.AddModelError(
                    "",
                    result?.Message ?? "Cannot create ticket type.");

                return View(model);
            }

            return RedirectToAction(
                "TicketTypes",
                "Organizer",
                new { eventId });
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

            if (response == null || response.Data == null)
                return RedirectToAction("Events", "Organizer");

            var ticket = response.Data;

            ViewBag.EventId = ticket.EventId;
            ViewBag.Sections = await LoadSectionsAsync(client, ticket.EventId);

            var model = new UpdateTicketTypeRequest
            {
                Name = ticket.Name,
                Description = ticket.Description,
                Price = ticket.Price,
                Quantity = ticket.Quantity,
                SaleStartTime = ticket.SaleStartTime,
                SaleEndTime = ticket.SaleEndTime
            };

            ViewBag.TicketTypeId = ticket.Id;
            ViewBag.EventId = ticket.EventId;
            ViewBag.Section = ticket.Section;
            ViewBag.IsSeatRequired = ticket.IsSeatRequired;

            return View(model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, Guid eventId, UpdateTicketTypeRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TicketTypeId = id;
                ViewBag.EventId = eventId;
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
                ViewBag.EventId = eventId;
                ViewBag.Sections = await LoadSectionsAsync(client, eventId);

                ModelState.AddModelError("", result?.Message ?? "Cannot update ticket type.");

                return View(model);
            }

            return RedirectToAction(
                "TicketTypes",
                "Organizer",
                new { eventId });
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

            var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<TicketTypeResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                TempData["Error"] = result?.Message ?? "Cannot deactivate ticket type.";
            }
            else
            {
                TempData["Success"] = result.Message ?? "Ticket type deactivated successfully.";
            }

            return RedirectToAction(
                "TicketTypes",
                "Organizer",
                new { eventId });
        }

        private async Task<List<string>> LoadSectionsAsync(HttpClient client, Guid eventId)
        {
            var response = await client.GetAsync(
                $"api/OrganizerProfile/events/{eventId}/sections");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<List<string>>>();

            return result?.Data ?? new List<string>();
        }
    }
}