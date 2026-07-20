using Eventix.Share.Category;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Seat;
using Eventix.Share.SeatMap;
using Eventix.Share.TicketType;
using Eventix.Share.Venue;
using Eventix.Share.VenueZone;
using Eventix.Web.Models.EventWizard;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Web.Controllers
{
    public class EventWizardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventWizardController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Step1()
        {
            ViewBag.CurrentStep = 1;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new EventInfoViewModel
            {
                Categories = await LoadCategoriesAsync(client)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Step1(EventInfoViewModel model)
        {

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");


            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (model.StartTime <= DateTime.Now)
            {
                TempData["Error"] = "Start time must be in the future.";
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }

            if (model.EndTime <= model.StartTime)
            {
                TempData["Error"] = "End time must be after start time.";
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }


            if (!ModelState.IsValid)
            {
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }

            HttpContext.Session.SetString(
                "EventWizard_Info",
                JsonSerializer.Serialize(model));

            return RedirectToAction("Step2");
        }

        [HttpGet]
        public async Task<IActionResult> Step2()
        {
            ViewBag.CurrentStep = 2;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var eventInfo = HttpContext.Session.GetString("EventWizard_Info");

            if (string.IsNullOrWhiteSpace(eventInfo))
                return RedirectToAction("Step1");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new EventVenueViewModel
            {
                Venues = await LoadVenuesAsync(client)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Step3Continue()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var zones = await LoadVenueZonesAsync(client, venueId);

            if (!zones.Any())
            {
                TempData["Error"] = "Please create at least one venue zone before continuing.";
                return RedirectToAction("Step3");
            }

            return RedirectToAction("Step4");
        }

        [HttpPost]
        public async Task<IActionResult> Step2(EventVenueViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var eventInfo = HttpContext.Session.GetString("EventWizard_Info");

            if (string.IsNullOrWhiteSpace(eventInfo))
                return RedirectToAction("Step1");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (model.Mode == "select")
            {
                if (model.SelectedVenueId == null || model.SelectedVenueId == Guid.Empty)
                {
                    TempData["Error"] = "Please select a venue.";
                    model.Venues = await LoadVenuesAsync(client);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    model.SelectedVenueId.Value.ToString());

                return RedirectToAction("Step3");
            }

            if (model.Mode == "create")
            {
                if (string.IsNullOrWhiteSpace(model.NewVenue.Name))
                    ModelState.AddModelError("NewVenue.Name", "Venue name is required.");
                if (string.IsNullOrWhiteSpace(model.NewVenue.Address))
                    ModelState.AddModelError("NewVenue.Address", "Venue address is required.");
                if (string.IsNullOrWhiteSpace(model.NewVenue.City))
                    ModelState.AddModelError("NewVenue.City", "Venue city is required.");
                if (model.NewVenue.Capacity <= 0)
                    ModelState.AddModelError("NewVenue.Capacity", "Capacity must be greater than 0.");

                if (!ModelState.IsValid)
                {
                    model.Mode = "create";
                    model.Venues = await LoadVenuesAsync(client);
                    return View(model);
                }

                var response = await client.PostAsJsonAsync(
                    "api/Venue/create",
                    model.NewVenue);

                var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<VenueResponse>>();

                if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess || result.Data == null)
                {
                    ModelState.AddModelError("", result?.Message ?? "Cannot create venue.");
                    model.Mode = "create";
                    model.Venues = await LoadVenuesAsync(client);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    result.Data.Id.ToString());

                return RedirectToAction("Step3");
            }

            ModelState.AddModelError("", "Invalid venue mode.");
            model.Venues = await LoadVenuesAsync(client);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Step3()
        {
            ViewBag.CurrentStep = 3;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString =
                HttpContext.Session.GetString("EventWizard_VenueId");

            if (!Guid.TryParse(venueIdString, out var venueId))
                return RedirectToAction("Step2");

            var client =
                _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = await BuildStep3ModelAsync(
                client,
                venueId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateZone(
    EventZonesViewModel model)
        {
            ViewBag.CurrentStep = 3;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString =
                HttpContext.Session.GetString("EventWizard_VenueId");

            if (!Guid.TryParse(venueIdString, out var venueId))
                return RedirectToAction("Step2");

            var client =
                _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            model.NewZone ??= new CreateVenueZoneRequest();

            model.NewZone.Name =
                model.NewZone.Name?.Trim();

            if (string.IsNullOrWhiteSpace(model.NewZone.Name))
            {
                ModelState.AddModelError(
                    "NewZone.Name",
                    "Zone name is required.");
            }

            if (model.NewZone.HasSeats)
            {
                model.NewZone.Capacity = 0;

                // ModelState giữ giá trị cũ của form,
                // cần cập nhật lại thành 0.
                ModelState.Remove("NewZone.Capacity");
            }
            else if (model.NewZone.Capacity <= 0)
            {
                ModelState.AddModelError(
                    "NewZone.Capacity",
                    "Capacity must be greater than 0 for zones without assigned seats.");
            }

            if (model.NewZone.SortOrder < 0)
            {
                ModelState.AddModelError(
                    "NewZone.SortOrder",
                    "Sort order cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(model.NewZone.Color))
            {
                model.NewZone.Color = "#60A5FA";
                ModelState.Remove("NewZone.Color");
            }

            if (!ModelState.IsValid)
            {
                model = await BuildStep3ModelAsync(
                    client,
                    venueId,
                    model);

                return View("Step3", model);
            }

            try
            {
                var response = await client.PostAsJsonAsync(
                    $"api/VenueZone/venue/{venueId}",
                    model.NewZone);

                var result = await response.Content
                    .ReadFromJsonAsync<
                        ApiResponseModel<VenueZoneResponse>>();

                if (!response.IsSuccessStatusCode ||
                    result == null ||
                    !result.IsSuccess)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result?.Message ?? "Cannot create zone.");

                    model = await BuildStep3ModelAsync(
                        client,
                        venueId,
                        model);

                    return View("Step3", model);
                }

                TempData["Success"] =
                    "Zone created successfully.";

                return RedirectToAction("Step3");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Cannot connect to the Eventix API. Please try again.");
            }
            catch (NotSupportedException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The API returned an unsupported response.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The API returned an invalid response.");
            }

            model = await BuildStep3ModelAsync(
                client,
                venueId,
                model);

            return View("Step3", model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSeatTemplate()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Seat/template");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Cannot download seat template.";
                return RedirectToAction("Step3");
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "seat_import_template.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ImportSeats(EventSeatsViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                TempData["Error"] = "Please choose an Excel file.";
                return RedirectToAction("Step5");
            }

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var form = new MultipartFormDataContent();

            using var stream = model.ExcelFile.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(model.ExcelFile.ContentType);

            form.Add(fileContent, "File", model.ExcelFile.FileName);

            var response = await client.PostAsync(
                $"api/Seat/{venueId}/import-excel",
                form);

            var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<ImportSeatResult>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                TempData["Error"] = result?.Message ?? "Cannot import seats.";
                return RedirectToAction("Step5");
            }

            TempData["Success"] = "Seats imported successfully.";

            return RedirectToAction("Step5");
        }

        [HttpGet]
        public async Task<IActionResult> Step4()
        {
            ViewBag.CurrentStep = 4;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new EventTicketTypesViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                Zones = await LoadVenueZonesAsync(client, venueId)
            };

            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            model.TicketTypes = string.IsNullOrWhiteSpace(ticketTypesJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(ticketTypesJson) ?? new();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddTicketType(EventTicketTypesViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            var ticket = model.NewTicketType;

            if (string.IsNullOrWhiteSpace(ticket.Name))
            {
                TempData["Error"] = "Ticket type name is required.";
                return RedirectToAction("Step4");
            }

            if (ticket.VenueZoneId == null || ticket.VenueZoneId == Guid.Empty)
            {
                TempData["Error"] = "Please select a zone.";
                return RedirectToAction("Step4");
            }

            if (ticket.Quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than 0.";
                return RedirectToAction("Step4");
            }

            if (ticket.Price < 0)
            {
                TempData["Error"] = "Price cannot be negative.";
                return RedirectToAction("Step4");
            }

            if (ticket.SaleStartTime <= DateTime.Now)
            {
                TempData["Error"] = "Sale start time must be in the future.";
                return RedirectToAction("Step4");
            }

            if (ticket.SaleEndTime <= ticket.SaleStartTime)
            {
                TempData["Error"] = "Sale end time must be after sale start time.";
                return RedirectToAction("Step4");
            }

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var zones = await LoadVenueZonesAsync(client, venueId);

            var selectedZone = zones.FirstOrDefault(z => z.Id == ticket.VenueZoneId);

            if (selectedZone == null)
            {
                TempData["Error"] = "Please select a valid zone.";
                return RedirectToAction("Step4");
            }

            if (ticket.IsSeatRequired && !selectedZone.HasSeats)
            {
                TempData["Error"] = "This ticket requires seats, but the selected zone has no assigned seats.";
                return RedirectToAction("Step4");
            }

            if (!ticket.IsSeatRequired && selectedZone.HasSeats)
            {
                TempData["Error"] = "This zone has assigned seats. Please enable seat selection for this ticket.";
                return RedirectToAction("Step4");
            }

            if (ticket.Quantity > selectedZone.Capacity)
            {
                TempData["Error"] = "Ticket quantity cannot exceed zone capacity.";
                return RedirectToAction("Step4");
            }

            var currentZoneQuantity = ticketTypes
                .Where(t => t.VenueZoneId == ticket.VenueZoneId)
                .Sum(t => t.Quantity);

            if (currentZoneQuantity + ticket.Quantity > selectedZone.Capacity)
            {
                TempData["Error"] = "Total ticket quantity in this zone cannot exceed zone capacity.";
                return RedirectToAction("Step4");
            }

            ticket.Section = selectedZone.Name;
            ticket.VenueZoneId = selectedZone.Id;
            ticketTypes.Add(ticket);

            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));

            TempData["Success"] = "Ticket type added successfully.";

            return RedirectToAction("Step4");
        }
        [HttpPost]
        public IActionResult Step4Continue()
        {
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] = "Please add at least one ticket type before continuing.";
                return RedirectToAction("Step4");
            }

            return RedirectToAction("Step5");
        }

        [HttpPost]
        public async Task<IActionResult> SaveSeatMap([FromBody] List<VenueSectionLayoutRequest> request)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return BadRequest("Venue not selected.");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync(
                $"api/Venue/{venueId}/seat-map",
                request);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<List<VenueSectionLayoutResponse>>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                return BadRequest(result?.Message ?? "Cannot save seat map.");
            }

            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> Step5()
        {
            ViewBag.CurrentStep = 5;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new EventSeatAssignmentViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                SeatStatuses = await LoadSeatImportStatusAsync(client, venueId)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Step5Continue()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var statuses = await LoadSeatImportStatusAsync(client, venueId);

            var incomplete = statuses
                .Where(x => !x.Completed)
                .ToList();

            if (incomplete.Any())
            {
                TempData["Error"] = "Please complete seat assignment for all seated zones.";
                return RedirectToAction("Step5");
            }

            return RedirectToAction("Step6");
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSeats([FromBody] GenerateSeatsRequest request)
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return BadRequest("Venue not selected.");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                $"api/Seat/venue/{venueId}/generate",
                request);

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponseModel<ImportSeatResult>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                return BadRequest(result?.Message ?? "Cannot generate seats.");
            }

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Step6()
        {
            ViewBag.CurrentStep = 6;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var venueIdString = HttpContext.Session.GetString("EventWizard_VenueId");

            if (string.IsNullOrWhiteSpace(venueIdString))
                return RedirectToAction("Step2");

            var venueId = Guid.Parse(venueIdString);

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new EventSeatMapViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                Zones = await LoadVenueZonesAsync(client, venueId),
                Layouts = await LoadSeatMapAsync(client, venueId)
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Step6Continue()
        {
            return RedirectToAction("Step7");
        }

        [HttpGet]
        public async Task<IActionResult> Step7()
        {
            ViewBag.CurrentStep = 7;

            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var eventInfoJson =
                HttpContext.Session.GetString("EventWizard_Info");

            if (string.IsNullOrWhiteSpace(eventInfoJson))
            {
                TempData["Error"] =
                    "Event information was not found. Please complete Step 1 again.";

                return RedirectToAction("Step1");
            }

            var venueIdString =
                HttpContext.Session.GetString("EventWizard_VenueId");

            if (!Guid.TryParse(venueIdString, out var venueId))
            {
                TempData["Error"] =
                    "Venue information was not found.";

                return RedirectToAction("Step2");
            }

            var ticketTypesJson =
                HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] =
                    "Please add at least one ticket type.";

                return RedirectToAction("Step4");
            }

            EventInfoViewModel? eventInfo;
            List<CreateTicketTypeRequest> ticketTypes;

            try
            {
                eventInfo = JsonSerializer.Deserialize<EventInfoViewModel>(
                    eventInfoJson);

                ticketTypes =
                    JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(
                        ticketTypesJson)
                    ?? new List<CreateTicketTypeRequest>();
            }
            catch (JsonException)
            {
                TempData["Error"] =
                    "Wizard data is invalid. Please restart event creation.";

                return RedirectToAction("Step1");
            }

            if (eventInfo == null)
            {
                TempData["Error"] =
                    "Event information could not be loaded.";

                return RedirectToAction("Step1");
            }

            if (ticketTypes.Count == 0)
            {
                TempData["Error"] =
                    "Please add at least one ticket type.";

                return RedirectToAction("Step4");
            }

            var client =
                _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var venue = await LoadVenueAsync(client, venueId);
            var zones = await LoadVenueZonesAsync(client, venueId);
            var statuses =
                await LoadSeatImportStatusAsync(client, venueId);
            var layouts =
                await LoadSeatMapAsync(client, venueId);

            if (venue == null)
            {
                TempData["Error"] =
                    "The selected venue could not be loaded.";

                return RedirectToAction("Step2");
            }

            var model = new EventReviewViewModel
            {
                EventInfo = eventInfo,
                Venue = venue,
                Zones = zones,
                TicketTypes = ticketTypes,

                SeatStatuses = statuses
                    .Select(x => new SeatReviewItemViewModel
                    {
                        VenueZoneId = x.VenueZoneId,
                        ZoneName = x.ZoneName,
                        HasSeats = x.HasSeats,
                        Capacity = x.Capacity,
                        SeatCount = x.ImportedSeats,
                        Completed = x.Completed
                    })
                    .ToList(),

                HasSavedMap = layouts.Count > 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish()
        {
            var token = Request.Cookies[CookieNames.Token];

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var eventInfoJson =
                HttpContext.Session.GetString("EventWizard_Info");

            var venueIdString =
                HttpContext.Session.GetString("EventWizard_VenueId");

            var ticketTypesJson =
                HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(eventInfoJson))
            {
                TempData["Error"] =
                    "Event information was not found.";

                return RedirectToAction("Step1");
            }

            if (!Guid.TryParse(venueIdString, out var venueId))
            {
                TempData["Error"] =
                    "Venue information was not found.";

                return RedirectToAction("Step2");
            }

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] =
                    "Ticket type information was not found.";

                return RedirectToAction("Step4");
            }

            EventInfoViewModel? eventInfo;
            List<CreateTicketTypeRequest> ticketTypes;

            try
            {
                eventInfo = JsonSerializer.Deserialize<EventInfoViewModel>(
                    eventInfoJson);

                ticketTypes =
                    JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(
                        ticketTypesJson)
                    ?? new List<CreateTicketTypeRequest>();
            }
            catch (JsonException)
            {
                TempData["Error"] =
                    "Wizard data is invalid.";

                return RedirectToAction("Step1");
            }

            if (eventInfo == null)
            {
                TempData["Error"] =
                    "Event information is invalid.";

                return RedirectToAction("Step1");
            }

            if (ticketTypes.Count == 0)
            {
                TempData["Error"] =
                    "At least one ticket type is required.";

                return RedirectToAction("Step4");
            }

            var client =
                _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            /*
             * Kiểm tra toàn bộ dữ liệu lần cuối
             * trước khi tạo Event.
             */
            var validationResult =
                await ValidateWizardBeforePublishAsync(
                    client,
                    venueId,
                    ticketTypes);

            if (!validationResult.IsValid)
            {
                TempData["Error"] = validationResult.Message;
                return RedirectToAction("Step7");
            }

            Guid? createdEventId = null;

            try
            {
                /*
                 * Bước 1: tạo Event ở trạng thái Draft.
                 */
                var createEventRequest =
                    BuildCreateEventRequest(eventInfo, venueId);

                var createEventResponse =
                    await client.PostAsJsonAsync(
                        "api/Events/create",
                        createEventRequest);

                var createEventResult =
                    await createEventResponse.Content
                        .ReadFromJsonAsync<
                            ApiResponseModel<EventDetailResponse>>();

                if (!createEventResponse.IsSuccessStatusCode ||
                    createEventResult == null ||
                    !createEventResult.IsSuccess ||
                    createEventResult.Data == null)
                {
                    TempData["Error"] =
                        createEventResult?.Message ??
                        "Cannot create event.";

                    return RedirectToAction("Step7");
                }

                createdEventId = createEventResult.Data.Id;

                /*
                 * Lưu tạm để có thể thử lại nếu bước tạo vé
                 * hoặc publish thất bại.
                 */
                HttpContext.Session.SetString("EventWizard_EventId", createdEventId.Value.ToString());

                /*
                 * Bước 2: tạo từng TicketType.
                 */
                foreach (var ticketType in ticketTypes)
                {
                    var ticketResponse =
                        await client.PostAsJsonAsync($"api/TicketType/event/{createdEventId.Value}", ticketType);

                    var ticketResult =
                        await ticketResponse.Content
                            .ReadFromJsonAsync<
                                ApiResponseModel<TicketTypeResponse>>();

                    if (!ticketResponse.IsSuccessStatusCode ||
                        ticketResult == null ||
                        !ticketResult.IsSuccess)
                    {
                        TempData["Error"] = ticketResult?.Message ?? $"Cannot create ticket type '{ticketType.Name}'.";

                        return RedirectToAction("Step7");
                    }
                }

                /*
                 * Bước 3: publish Event.
                 *
                 * Controller backend là EventsController,
                 * nên endpoint phải là api/Events, không phải api/Event.
                 */


                var publishResponse = await client.PostAsync(
                    $"api/Events/{createdEventId}/publish",
                    null);

                Console.WriteLine($"Status: {publishResponse.StatusCode}");
                Console.WriteLine($"Reason: {publishResponse.ReasonPhrase}");

                var errorContent = await publishResponse.Content.ReadAsStringAsync();


                Console.WriteLine(errorContent);

                var publishResult =
                    await publishResponse.Content
                        .ReadFromJsonAsync<
                            ApiResponseModel<EventResponse>>();

                if (!publishResponse.IsSuccessStatusCode ||
                    publishResult == null ||
                    !publishResult.IsSuccess)
                {
                    TempData["Error"] =
                        publishResult?.Message ??
                        "Event was created, but it could not be published.";

                    return RedirectToAction("Step7");
                }

                ClearEventWizardSession();

                TempData["Success"] = "Event published successfully.";

                return RedirectToAction("ManageEvent", "Organizer", new { id = createdEventId.Value });
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = createdEventId.HasValue
                        ? "The event was created as a draft, but the remaining publishing steps failed."
                        : "Cannot connect to the Eventix API.";

                return RedirectToAction("Step7");
            }
            catch (JsonException)
            {
                TempData["Error"] = "The API returned an invalid response.";

                return RedirectToAction("Step7");
            }
        }

        private static CreateEventRequest BuildCreateEventRequest(EventInfoViewModel eventInfo, Guid venueId)
        {
            return new CreateEventRequest
            {
                CategoryId = eventInfo.CategoryId,
                VenueId = venueId,

                Title = eventInfo.Title?.Trim() ?? string.Empty,
                Description = eventInfo.Description,
                Summary = eventInfo.Summary,

                ImageUrl = eventInfo.ImageUrl,
                BannerUrl = eventInfo.BannerUrl,

                StartTime = eventInfo.StartTime,
                EndTime = eventInfo.EndTime,

                Status = "Draft",
                IsFeatured = false,
                PublishedAt = null
            };
        }

        private async Task<EventZonesViewModel> BuildStep3ModelAsync(HttpClient client, Guid venueId, EventZonesViewModel? postedModel = null)
        {
            return new EventZonesViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                Zones = await LoadVenueZonesAsync(client, venueId),

                // Giữ lại dữ liệu người dùng vừa nhập
                NewZone = postedModel?.NewZone ?? new CreateVenueZoneRequest
                {
                    HasSeats = true,
                    Capacity = 0,
                    Color = "#60A5FA",
                    SortOrder = 1
                }
            };
        }

        private async Task<List<VenueResponse>> LoadVenuesAsync(HttpClient client)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<VenueResponse>>>(
                "api/Venue/venues");

            return response?.Data?.DataList ?? new List<VenueResponse>();
        }
        private async Task<List<CategoryResponse>> LoadCategoriesAsync(HttpClient client)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<CategoryResponse>>>(
                "api/category/categories");

            return response?.Data?.DataList ?? new List<CategoryResponse>();
        }
        private async Task<VenueResponse?> LoadVenueAsync(HttpClient client, Guid venueId)
        {
            var response = await client.GetFromJsonAsync<ApiResponseModel<VenueResponse>>(
                $"api/Venue/{venueId}");

            return response?.Data;
        }

        private async Task<List<SeatSectionResponse>> LoadSectionsByVenueAsync(HttpClient client, Guid venueId)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<List<SeatSectionResponse>>>(
                $"api/Seat/venue/{venueId}/sections");

            return response?.Data ?? new List<SeatSectionResponse>();
        }

        private async Task<List<VenueSectionLayoutResponse>> LoadSeatMapAsync(HttpClient client, Guid venueId)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<List<VenueSectionLayoutResponse>>>(
                $"api/Venue/{venueId}/seat-map");

            return response?.Data ?? new List<VenueSectionLayoutResponse>();
        }
        private async Task<List<VenueZoneResponse>> LoadVenueZonesAsync(HttpClient client, Guid venueId)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<List<VenueZoneResponse>>>(
                $"api/VenueZone/venue/{venueId}");

            return response?.Data ?? new List<VenueZoneResponse>();
        }
        private async Task<List<SeatImportStatusResponse>> LoadSeatImportStatusAsync(HttpClient client, Guid venueId)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<List<SeatImportStatusResponse>>>(
                $"api/VenueZone/venue/{venueId}/seat-import-status");

            return response?.Data ?? new List<SeatImportStatusResponse>();
        }

        private void ClearEventWizardSession()
        {
            HttpContext.Session.Remove("EventWizard_Info");
            HttpContext.Session.Remove("EventWizard_VenueId");
            HttpContext.Session.Remove("EventWizard_TicketTypes");
            HttpContext.Session.Remove("EventWizard_EventId");
        }

        private async Task<(bool IsValid, string Message)> ValidateWizardBeforePublishAsync(
        HttpClient client,
        Guid venueId,
        List<CreateTicketTypeRequest> ticketTypes)
        {
            var venue = await LoadVenueAsync(client, venueId);

            if (venue == null)
                return (false, "The selected venue does not exist.");

            var zones =
                await LoadVenueZonesAsync(client, venueId);

            if (zones.Count == 0)
                return (false, "At least one venue zone is required.");

            var seatStatuses =
                await LoadSeatImportStatusAsync(client, venueId);

            var incompleteZones = seatStatuses
                .Where(x => x.HasSeats && !x.Completed)
                .Select(x => x.ZoneName)
                .ToList();

            if (incompleteZones.Count > 0)
            {
                return (
                    false,
                    $"Seat assignment is incomplete for: " +
                    $"{string.Join(", ", incompleteZones)}."
                );
            }

            var totalCapacity = zones.Sum(x => x.Capacity);

            if (totalCapacity > venue.Capacity)
            {
                return (
                    false,
                    $"Total zone capacity ({totalCapacity}) exceeds " +
                    $"venue capacity ({venue.Capacity})."
                );
            }

            var layouts =
                await LoadSeatMapAsync(client, venueId);

            var mappedSections = layouts
                .Select(x => x.Section)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var zonesWithoutLayout = zones
                .Where(z => !mappedSections.Contains(z.Name))
                .Select(z => z.Name)
                .ToList();

            if (zonesWithoutLayout.Count > 0)
            {
                return (
                    false,
                    $"Map layout is missing for: " +
                    $"{string.Join(", ", zonesWithoutLayout)}."
                );
            }

            foreach (var ticket in ticketTypes)
            {
                if (!ticket.VenueZoneId.HasValue)
                {
                    return (
                        false,
                        $"Ticket type '{ticket.Name}' has no venue zone."
                    );
                }

                var zone = zones.FirstOrDefault(
                    x => x.Id == ticket.VenueZoneId.Value);

                if (zone == null)
                {
                    return (
                        false,
                        $"Ticket type '{ticket.Name}' has an invalid zone."
                    );
                }

                if (ticket.Quantity <= 0)
                {
                    return (
                        false,
                        $"Ticket type '{ticket.Name}' has an invalid quantity."
                    );
                }

                if (ticket.IsSeatRequired != zone.HasSeats)
                {
                    return (
                        false,
                        $"Ticket type '{ticket.Name}' does not match " +
                        $"zone '{zone.Name}'."
                    );
                }
            }

            var ticketGroups = ticketTypes
                .Where(x => x.VenueZoneId.HasValue)
                .GroupBy(x => x.VenueZoneId!.Value);

            foreach (var group in ticketGroups)
            {
                var zone = zones.FirstOrDefault(
                    x => x.Id == group.Key);

                if (zone == null)
                    return (false, "A ticket type has an invalid zone.");

                var totalTicketQuantity =
                    group.Sum(x => x.Quantity);

                if (totalTicketQuantity > zone.Capacity)
                {
                    return (
                        false,
                        $"Total ticket quantity in zone '{zone.Name}' " +
                        $"exceeds its capacity."
                    );
                }
            }

            return (true, string.Empty);
        }
    }
}