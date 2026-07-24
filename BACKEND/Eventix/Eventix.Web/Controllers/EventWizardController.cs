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

            // Safety net: nếu DateTime được deserialize với Kind = Unspecified
            // (trường hợp browser không support ISO string), ép về UTC.
            if (model.StartTime.Kind == DateTimeKind.Unspecified)
                model.StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Utc);

            if (model.EndTime.Kind == DateTimeKind.Unspecified)
                model.EndTime = DateTime.SpecifyKind(model.EndTime, DateTimeKind.Utc);

            // Kiểm tra ModelState trước (bao gồm [Required] và binding errors)
            if (!ModelState.IsValid)
            {
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }

            // Business rules sau khi model hợp lệ
            if (model.StartTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(model.StartTime), "Start time must be in the future.");
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }

            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(nameof(model.EndTime), "End time must be after start time.");
                model.Categories = await LoadCategoriesAsync(client);
                return View(model);
            }

            // Xóa Categories trước khi lưu vào session (lookup data, không cần persist)
            model.Categories = new();

            HttpContext.Session.SetString(
                "EventWizard_Info",
                JsonSerializer.Serialize(model));

            return RedirectToAction("Step2");
        }

        [HttpGet]
        public async Task<IActionResult> Step2(int venuePage = 1)
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
                VenuePage = await LoadVenuesAsync(client, venuePage)
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    model.SelectedVenueId.Value.ToString());
                InvalidateSeatMapSave();

                // Skip Step 3 (Venue Zones) - go directly to Ticket Types
                return RedirectToAction("Step4");
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage);
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    result.Data.Id.ToString());
                InvalidateSeatMapSave();

                // Skip Step 3 (Venue Zones) - go directly to Ticket Types
                return RedirectToAction("Step4");
            }

            ModelState.AddModelError("", "Invalid venue mode.");
            model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage);
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
            ViewBag.CurrentStep = 3; // Renumbered: step 3 (Zones) removed, Ticket Types is now step 3

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
                Venue = await LoadVenueAsync(client, venueId)
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
            ViewBag.CurrentStep = 3;

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

            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            var ticket = model.NewTicketType;

            // Safety net: ép Kind = UTC (ISO string từ browser parse thành Utc tự động,
            // nhưng phòng trường hợp model binder trả về Unspecified)
            if (ticket.SaleStartTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleStartTime = DateTime.SpecifyKind(ticket.SaleStartTime, DateTimeKind.Utc);
            if (ticket.SaleEndTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleEndTime = DateTime.SpecifyKind(ticket.SaleEndTime, DateTimeKind.Utc);

            // ── Validate từng trường, thêm lỗi vào ModelState ────────────────
            if (string.IsNullOrWhiteSpace(ticket.Name))
                ModelState.AddModelError("NewTicketType.Name", "Ticket type name is required.");
            else if (ticketTypes.Any(t => string.Equals(t.Name.Trim(), ticket.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("NewTicketType.Name", $"A ticket type named '{ticket.Name}' already exists.");

            if (ticket.Quantity <= 0)
                ModelState.AddModelError("NewTicketType.Quantity", "Quantity must be greater than 0.");

            if (ticket.Price < 0)
                ModelState.AddModelError("NewTicketType.Price", "Price cannot be negative.");

            if (ticket.SaleStartTime == default)
                ModelState.AddModelError("NewTicketType.SaleStartTime", "Sale start time is required.");
            else if (ticket.SaleStartTime <= DateTime.UtcNow)
                ModelState.AddModelError("NewTicketType.SaleStartTime", "Sale start time must be in the future.");

            if (ticket.SaleEndTime == default)
                ModelState.AddModelError("NewTicketType.SaleEndTime", "Sale end time is required.");
            else if (ticket.SaleStartTime != default && ticket.SaleEndTime <= ticket.SaleStartTime)
                ModelState.AddModelError("NewTicketType.SaleEndTime", "Sale end time must be after sale start time.");

            // Kiểm tra tổng quantity không vượt venue capacity
            if (ticket.Quantity > 0)
            {
                var venue = await LoadVenueAsync(client, venueId);
                if (venue != null)
                {
                    var currentTotal = ticketTypes.Sum(t => t.Quantity);
                    if (currentTotal + ticket.Quantity > venue.Capacity)
                        ModelState.AddModelError("NewTicketType.Quantity",
                            $"Total quantity ({currentTotal + ticket.Quantity}) would exceed venue capacity ({venue.Capacity}).");
                }
            }

            // ── Nếu có lỗi: trả về View với model đầy đủ ─────────────────────
            if (!ModelState.IsValid)
            {
                model.VenueId = venueId;
                model.Venue = await LoadVenueAsync(client, venueId);
                model.TicketTypes = ticketTypes;
                return View("Step4", model);
            }

            // ── Thành công: lưu vào session ───────────────────────────────────
            ticketTypes.Add(ticket);
            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

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

            HttpContext.Session.SetString("EventWizard_SeatMapSaved", "true");
            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> Step5()
        {
            // Step 5 is now "Seat Preview" - shows which ticket types will have seats auto-generated
            // Actual seat generation happens during Publish (TicketTypeService auto-generates seats)
            ViewBag.CurrentStep = 4; // Renumbered: step 3 removed, so seat preview is visual step 4

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

            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            var ticketTypes = string.IsNullOrWhiteSpace(ticketTypesJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(ticketTypesJson) ?? new();

            var model = new EventSeatAssignmentViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                TicketTypes = ticketTypes
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Step5Continue()
        {
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] = "Please add at least one ticket type before continuing.";
                return RedirectToAction("Step4");
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
            ViewBag.CurrentStep = 5; // Renumbered: step 3 removed
            ViewBag.IsSeatMapSaved = IsSeatMapSaved();

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

            // Load ticket types from session to build preview sections for the map
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            var ticketTypes = string.IsNullOrWhiteSpace(ticketTypesJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(ticketTypesJson) ?? new();

            // Build preview sections from ticket types (map will be arranged after publish)
            // Use VenueSectionLayouts from the venue if they exist (from previous events)
            var layouts = await LoadSeatMapAsync(client, venueId);

            // Filter layouts to only those matching current ticket type names
            var ticketTypeNames = ticketTypes.Select(t => t.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filteredLayouts = layouts
                .Where(l => ticketTypeNames.Contains(l.Section ?? string.Empty))
                .ToList();

            var model = new EventSeatMapViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                TicketTypes = ticketTypes,
                Layouts = filteredLayouts
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Step6Continue()
        {
            if (!IsSeatMapSaved())
            {
                TempData["Error"] = "Please save the venue map before continuing to review.";
                return RedirectToAction("Step6");
            }

            return RedirectToAction("Step7");
        }

        [HttpGet]
        public async Task<IActionResult> Step7()
        {
            ViewBag.CurrentStep = 6; // Renumbered: step 3 removed

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

            if (venue == null)
            {
                TempData["Error"] =
                    "The selected venue could not be loaded.";

                return RedirectToAction("Step2");
            }

            // Load categories để dropdown trong inline-edit form có dữ liệu
            eventInfo.Categories = await LoadCategoriesAsync(client);

            var model = new EventReviewViewModel
            {
                EventInfo = eventInfo,
                Venue = venue,
                TicketTypes = ticketTypes
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

            if (!IsSeatMapSaved())
            {
                TempData["Error"] =
                    "Please save the venue map before publishing the event.";

                return RedirectToAction("Step6");
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

        // ── Inline-edit actions cho Review page (Step 7) ──────────────────────

        /// <summary>
        /// AJAX POST: cập nhật thông tin sự kiện (EventInfo) trong session từ Review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEventInfo(EventInfoViewModel model)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(model.Title))
                return Json(new { success = false, message = "Event title is required." });

            if (model.CategoryId == Guid.Empty)
                return Json(new { success = false, message = "Category is required." });

            if (model.StartTime <= DateTime.UtcNow)
                return Json(new { success = false, message = "Start time must be in the future." });

            if (model.EndTime <= model.StartTime)
                return Json(new { success = false, message = "End time must be after start time." });

            // Xóa Categories trước khi lưu vào session (lookup data, load lại từ API khi cần)
            model.Categories = new();

            HttpContext.Session.SetString(
                "EventWizard_Info",
                JsonSerializer.Serialize(model));

            return Json(new { success = true, message = "Event information updated." });
        }

        /// <summary>
        /// AJAX POST: thêm ticket type mới từ Review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTicketTypeFromReview([FromBody] CreateTicketTypeRequest ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.Name))
                return Json(new { success = false, message = "Ticket name is required." });

            if (ticket.Quantity <= 0)
                return Json(new { success = false, message = "Quantity must be greater than 0." });

            if (ticket.Price < 0)
                return Json(new { success = false, message = "Price cannot be negative." });

            if (ticket.SaleStartTime <= DateTime.UtcNow)
                return Json(new { success = false, message = "Sale start time must be in the future." });

            if (ticket.SaleEndTime <= ticket.SaleStartTime)
                return Json(new { success = false, message = "Sale end time must be after sale start time." });

            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            if (ticketTypes.Any(t => string.Equals(t.Name.Trim(), ticket.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, message = $"A ticket type named '{ticket.Name}' already exists." });

            ticketTypes.Add(ticket);

            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            return Json(new { success = true, message = "Ticket type added.", count = ticketTypes.Count });
        }

        /// <summary>
        /// AJAX POST: xóa ticket type theo index từ Review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveTicketTypeFromReview([FromBody] int index)
        {
            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            if (index < 0 || index >= ticketTypes.Count)
                return Json(new { success = false, message = "Invalid ticket index." });

            if (ticketTypes.Count == 1)
                return Json(new { success = false, message = "At least one ticket type is required." });

            ticketTypes.RemoveAt(index);

            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            return Json(new { success = true, message = "Ticket type removed.", count = ticketTypes.Count });
        }

        /// <summary>
        /// AJAX POST: cập nhật ticket type theo index từ Review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTicketTypeFromReview([FromBody] UpdateTicketTypeFromReviewRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request." });

            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            if (request.Index < 0 || request.Index >= ticketTypes.Count)
                return Json(new { success = false, message = "Invalid ticket index." });

            var ticket = request.Ticket;

            // Validate
            if (string.IsNullOrWhiteSpace(ticket?.Name))
                return Json(new { success = false, field = "Name", message = "Ticket name is required." });

            // Safety net: ép Kind = UTC
            if (ticket.SaleStartTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleStartTime = DateTime.SpecifyKind(ticket.SaleStartTime, DateTimeKind.Utc);
            if (ticket.SaleEndTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleEndTime = DateTime.SpecifyKind(ticket.SaleEndTime, DateTimeKind.Utc);

            // Kiểm tra tên trùng (bỏ qua chính nó)
            if (ticketTypes
                .Where((_, i) => i != request.Index)
                .Any(t => string.Equals(t.Name.Trim(), ticket.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, field = "Name", message = $"A ticket type named '{ticket.Name}' already exists." });

            if (ticket.Quantity <= 0)
                return Json(new { success = false, field = "Quantity", message = "Quantity must be greater than 0." });

            if (ticket.Price < 0)
                return Json(new { success = false, field = "Price", message = "Price cannot be negative." });

            if (ticket.SaleStartTime == default)
                return Json(new { success = false, field = "SaleStartTime", message = "Sale start time is required." });

            if (ticket.SaleStartTime <= DateTime.UtcNow)
                return Json(new { success = false, field = "SaleStartTime", message = "Sale start time must be in the future." });

            if (ticket.SaleEndTime == default)
                return Json(new { success = false, field = "SaleEndTime", message = "Sale end time is required." });

            if (ticket.SaleEndTime <= ticket.SaleStartTime)
                return Json(new { success = false, field = "SaleEndTime", message = "Sale end time must be after sale start time." });

            // Cập nhật
            var existing = ticketTypes[request.Index];
            existing.Name = ticket.Name.Trim();
            existing.Description = ticket.Description;
            existing.Price = ticket.Price;
            existing.Quantity = ticket.Quantity;
            existing.SaleStartTime = ticket.SaleStartTime;
            existing.SaleEndTime = ticket.SaleEndTime;
            existing.IsSeatRequired = ticket.IsSeatRequired;

            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            return Json(new { success = true, message = "Ticket type updated.", count = ticketTypes.Count });
        }

        private static CreateEventRequest BuildCreateEventRequest(EventInfoViewModel eventInfo, Guid venueId)
        {
            return new CreateEventRequest
            {
                CategoryId = eventInfo.CategoryId ?? Guid.Empty,
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

        private async Task<PaginationResponse<VenueResponse>> LoadVenuesAsync(HttpClient client, int page = 1, int pageSize = 6)
        {
            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<VenueResponse>>>(
                $"api/Venue/venues?CurrentPage={page}&PageSize={pageSize}");

            return response?.Data ?? new PaginationResponse<VenueResponse>();
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
            HttpContext.Session.Remove("EventWizard_SeatMapSaved");
        }

        private bool IsSeatMapSaved() =>
            HttpContext.Session.GetString("EventWizard_SeatMapSaved") == "true";

        private void InvalidateSeatMapSave() =>
            HttpContext.Session.Remove("EventWizard_SeatMapSaved");

        // Proxy upload ảnh: Web nhận file → gọi API → trả URL về browser
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadEventImage(IFormFile? file)
        {
            var token = Request.Cookies[CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { success = false, message = "Not logged in." });

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await client.PostAsync("api/events/upload-image", content);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode,
                        new { success = false, message = $"API error {(int)response.StatusCode}: {raw}" });

                var json = System.Text.Json.JsonSerializer.Deserialize<ApiResponseModel<UploadImageProxyResponse>>(
                    raw,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (json == null || !json.IsSuccess || json.Data == null)
                    return BadRequest(new { success = false, message = json?.Message ?? "Upload failed." });

                var relativeUrl = json.Data.RelativeUrl;

                var fullUrl = json.Data.Url;

                if (string.IsNullOrWhiteSpace(fullUrl))
                {
                    fullUrl = $"https://localhost:7162{relativeUrl}";
                }
                else if (!fullUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    fullUrl = $"https://localhost:7162{fullUrl}";
                }

                return Ok(new
                {
                    success = true,
                    relativeUrl,
                    url = fullUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private sealed class UploadImageProxyResponse
        {
            public string Url { get; set; } = string.Empty;
            public string RelativeUrl { get; set; } = string.Empty;
        }

        // DTO nội bộ cho UpdateTicketTypeFromReview
        public sealed class UpdateTicketTypeFromReviewRequest
        {
            public int Index { get; set; }
            public CreateTicketTypeRequest? Ticket { get; set; }
        }

        private async Task<(bool IsValid, string Message)> ValidateWizardBeforePublishAsync(
        HttpClient client,
        Guid venueId,
        List<CreateTicketTypeRequest> ticketTypes)
        {
            var venue = await LoadVenueAsync(client, venueId);

            if (venue == null)
                return (false, "The selected venue does not exist.");

            if (ticketTypes.Count == 0)
                return (false, "At least one ticket type is required.");

            foreach (var ticket in ticketTypes)
            {
                if (string.IsNullOrWhiteSpace(ticket.Name))
                    return (false, "All ticket types must have a name.");

                if (ticket.Quantity <= 0)
                    return (false, $"Ticket type '{ticket.Name}' has an invalid quantity.");

                if (ticket.Price < 0)
                    return (false, $"Ticket type '{ticket.Name}' has an invalid price.");

                if (ticket.SaleStartTime >= ticket.SaleEndTime)
                    return (false, $"Ticket type '{ticket.Name}' has invalid sale times.");
            }

            return (true, string.Empty);
        }
    }
}
