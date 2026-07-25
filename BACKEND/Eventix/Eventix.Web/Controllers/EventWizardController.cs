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
        public async Task<IActionResult> Step2(int venuePage = 1, string? venueSearch = null)
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
                VenueSearch = venueSearch,
                VenuePage = await LoadVenuesAsync(client, venuePage, search: venueSearch)
            };

            return View(model);
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage, search: model.VenueSearch);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    model.SelectedVenueId.Value.ToString());
                InvalidateSeatMapSave();

                // Skip Step 3 (Venue Zones) - go directly to Ticket Types
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage, search: model.VenueSearch);
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
                    model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage, search: model.VenueSearch);
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "EventWizard_VenueId",
                    result.Data.Id.ToString());
                InvalidateSeatMapSave();

                // Skip Step 3 (Venue Zones) - go directly to Ticket Types
                return RedirectToAction("Step3");
            }

            ModelState.AddModelError("", "Invalid venue mode.");
            model.VenuePage = await LoadVenuesAsync(client, model.VenuePage.CurrentPage, search: model.VenueSearch);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Step3()
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

            var model = new EventTicketTypesViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId)
            };

            // Lấy EventStartTime từ session để giới hạn SaleEndTime
            var eventInfoJson = HttpContext.Session.GetString("EventWizard_Info");
            if (!string.IsNullOrWhiteSpace(eventInfoJson))
            {
                var eventInfo = JsonSerializer.Deserialize<EventInfoViewModel>(eventInfoJson);
                if (eventInfo?.StartTime != default)
                    model.EventStartTime = eventInfo.StartTime;
            }

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
            else
            {
                // Validate SaleEndTime phải <= EventStartTime (rule từ backend TicketTypeService)
                var eventInfoJson = HttpContext.Session.GetString("EventWizard_Info");
                if (!string.IsNullOrWhiteSpace(eventInfoJson))
                {
                    var eventInfo = JsonSerializer.Deserialize<EventInfoViewModel>(eventInfoJson);
                    if (eventInfo != null && eventInfo.StartTime != default && ticket.SaleEndTime > eventInfo.StartTime)
                        ModelState.AddModelError("NewTicketType.SaleEndTime",
                            $"Thời gian kết thúc bán phải trước thời gian bắt đầu sự kiện ({eventInfo.StartTime:dd/MM/yyyy HH:mm} UTC).");
                }
            }

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
                return View("Step3", model);
            }

            // ── Thành công: lưu vào session ───────────────────────────────────
            ticketTypes.Add(ticket);
            HttpContext.Session.SetString(
                "EventWizard_TicketTypes",
                JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            TempData["Success"] = "Ticket type added successfully.";
            return RedirectToAction("Step3");
        }
        [HttpPost]
        public IActionResult Step3Continue()
        {
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] = "Please add at least one ticket type before continuing.";
                return RedirectToAction("Step3");
            }

            return RedirectToAction("Step4");
        }

        /// <summary>
        /// AJAX POST: xóa ticket type theo index tại Step3.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveTicketType([FromBody] int index)
        {
            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            if (index < 0 || index >= ticketTypes.Count)
                return Json(new { success = false, message = "Chỉ số vé không hợp lệ." });

            if (ticketTypes.Count == 1)
                return Json(new { success = false, message = "Cần ít nhất một loại vé." });

            ticketTypes.RemoveAt(index);
            HttpContext.Session.SetString("EventWizard_TicketTypes", JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            return Json(new { success = true, message = "Đã xóa loại vé.", count = ticketTypes.Count });
        }

        /// <summary>
        /// AJAX POST: cập nhật ticket type theo index tại Step3.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTicketType([FromBody] EventWizardController.UpdateTicketTypeFromReviewRequest request)
        {
            if (request?.Ticket == null)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var currentJson = HttpContext.Session.GetString("EventWizard_TicketTypes");
            var ticketTypes = string.IsNullOrWhiteSpace(currentJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(currentJson) ?? new();

            if (request.Index < 0 || request.Index >= ticketTypes.Count)
                return Json(new { success = false, message = "Chỉ số vé không hợp lệ." });

            var ticket = request.Ticket;

            // Safety net: ép Kind = UTC
            if (ticket.SaleStartTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleStartTime = DateTime.SpecifyKind(ticket.SaleStartTime, DateTimeKind.Utc);
            if (ticket.SaleEndTime.Kind == DateTimeKind.Unspecified)
                ticket.SaleEndTime = DateTime.SpecifyKind(ticket.SaleEndTime, DateTimeKind.Utc);

            if (string.IsNullOrWhiteSpace(ticket.Name))
                return Json(new { success = false, field = "Name", message = "Tên loại vé là bắt buộc." });

            if (ticketTypes.Where((_, i) => i != request.Index)
                           .Any(t => string.Equals(t.Name.Trim(), ticket.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, field = "Name", message = $"Đã có loại vé tên '{ticket.Name}'." });

            if (ticket.Quantity <= 0)
                return Json(new { success = false, field = "Quantity", message = "Số lượng phải lớn hơn 0." });

            if (ticket.Price < 0)
                return Json(new { success = false, field = "Price", message = "Giá không được âm." });

            if (ticket.SaleStartTime == default)
                return Json(new { success = false, field = "SaleStartTime", message = "Bắt đầu bán là bắt buộc." });

            if (ticket.SaleEndTime == default)
                return Json(new { success = false, field = "SaleEndTime", message = "Kết thúc bán là bắt buộc." });

            if (ticket.SaleEndTime <= ticket.SaleStartTime)
                return Json(new { success = false, field = "SaleEndTime", message = "Kết thúc bán phải sau bắt đầu bán." });

            // SaleEndTime phải <= EventStartTime
            var eventInfoJsonU = HttpContext.Session.GetString("EventWizard_Info");
            if (!string.IsNullOrWhiteSpace(eventInfoJsonU))
            {
                var eventInfoU = JsonSerializer.Deserialize<EventInfoViewModel>(eventInfoJsonU);
                if (eventInfoU != null && eventInfoU.StartTime != default && ticket.SaleEndTime > eventInfoU.StartTime)
                    return Json(new { success = false, field = "SaleEndTime",
                        message = $"Kết thúc bán phải trước thời gian bắt đầu sự kiện ({eventInfoU.StartTime:dd/MM/yyyy HH:mm} UTC)." });
            }

            var existing = ticketTypes[request.Index];
            existing.Name          = ticket.Name.Trim();
            existing.Description   = ticket.Description;
            existing.Price         = ticket.Price;
            existing.Quantity      = ticket.Quantity;
            existing.SaleStartTime = ticket.SaleStartTime;
            existing.SaleEndTime   = ticket.SaleEndTime;
            existing.IsSeatRequired = ticket.IsSeatRequired;

            HttpContext.Session.SetString("EventWizard_TicketTypes", JsonSerializer.Serialize(ticketTypes));
            InvalidateSeatMapSave();

            return Json(new { success = true, message = "Đã cập nhật loại vé.", count = ticketTypes.Count });
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
        public async Task<IActionResult> Step4()
        {
            // Step 5 is now "Seat Preview" - shows which ticket types will have seats auto-generated
            // Actual seat generation happens during Publish (TicketTypeService auto-generates seats)
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
        public IActionResult Step4Continue()
        {
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            if (string.IsNullOrWhiteSpace(ticketTypesJson))
            {
                TempData["Error"] = "Please add at least one ticket type before continuing.";
                return RedirectToAction("Step3");
            }

            return RedirectToAction("Step5");
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
        public async Task<IActionResult> Step5()
        {
            ViewBag.CurrentStep = 5;
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

            // Ticket types từ session — đây là nguồn duy nhất để tạo các block trên map.
            var ticketTypesJson = HttpContext.Session.GetString("EventWizard_TicketTypes");

            var ticketTypes = string.IsNullOrWhiteSpace(ticketTypesJson)
                ? new List<CreateTicketTypeRequest>()
                : JsonSerializer.Deserialize<List<CreateTicketTypeRequest>>(ticketTypesJson) ?? new();

            // Load layouts đã lưu từ venue — chỉ dùng để khôi phục vị trí/kích thước
            // của block có tên trùng với ticket type. JS sẽ bỏ qua layout không khớp tên.
            var layouts = await LoadSeatMapAsync(client, venueId);

            var model = new EventSeatMapViewModel
            {
                VenueId = venueId,
                Venue = await LoadVenueAsync(client, venueId),
                TicketTypes = ticketTypes,
                Layouts = layouts   // truyền hết, JS tự tra cứu theo tên
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Step5Continue()
        {
            if (!IsSeatMapSaved())
            {
                TempData["Error"] = "Please save the venue map before continuing to review.";
                return RedirectToAction("Step5");
            }

            return RedirectToAction("Step6");
        }

        [HttpGet]
        public async Task<IActionResult> Step6()
        {
            ViewBag.CurrentStep = 6;

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

                return RedirectToAction("Step3");
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

                return RedirectToAction("Step3");
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

                return RedirectToAction("Step3");
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

                return RedirectToAction("Step3");
            }

            if (!IsSeatMapSaved())
            {
                TempData["Error"] =
                    "Please save the venue map before publishing the event.";

                return RedirectToAction("Step5");
            }

            var client =
                _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            /*
             * Auto-adjust SaleStartTime nếu đã qua hiện tại.
             * Lý do: organizer có thể tạo wizard từ hôm qua, hoặc set SaleStartTime
             * ngay lúc tạo vé nhưng đến lúc Publish thì giờ đó đã qua.
             * → Thay vì báo lỗi, tự động điều chỉnh về UTC now để mở bán ngay khi publish.
             */
            var publishNow = DateTime.UtcNow;
            foreach (var tt in ticketTypes)
            {
                if (tt.SaleStartTime.Kind == DateTimeKind.Unspecified)
                    tt.SaleStartTime = DateTime.SpecifyKind(tt.SaleStartTime, DateTimeKind.Utc);
                if (tt.SaleStartTime <= publishNow)
                    tt.SaleStartTime = publishNow.AddSeconds(5); // nhỏ hơn 1 phút, mở bán gần như ngay lập tức
            }
            // Lưu lại session sau khi adjust
            HttpContext.Session.SetString("EventWizard_TicketTypes", JsonSerializer.Serialize(ticketTypes));

            /*
             * Kiểm tra toàn bộ dữ liệu lần cuối
             * trước khi tạo Event — bao gồm SaleEndTime vs EventStartTime.
             */
            var validationResult =
                await ValidateWizardBeforePublishAsync(
                    client,
                    venueId,
                    ticketTypes,
                    eventInfo.StartTime);

            if (!validationResult.IsValid)
            {
                TempData["Error"] = validationResult.Message;
                return RedirectToAction("Step6");
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

                    return RedirectToAction("Step6");
                }

                createdEventId = createEventResult.Data.Id;

                /*
                 * Lưu tạm để có thể thử lại nếu bước tạo vé
                 * hoặc publish thất bại.
                 */
                HttpContext.Session.SetString("EventWizard_EventId", createdEventId.Value.ToString());

                /*
                 * Bước 2: tạo từng TicketType.
                 * Nếu bất kỳ TicketType nào thất bại → rollback: xóa Event Draft vừa tạo.
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
                        // Rollback: xóa Event Draft để tránh event rỗng tồn tại trong DB
                        await TryDeleteDraftEventAsync(client, createdEventId.Value);

                        TempData["Error"] =
                            $"Loại vé '{ticketType.Name}': {ticketResult?.Message ?? "Không thể tạo loại vé."} — Sự kiện đã được hủy, vui lòng thử lại.";

                        return RedirectToAction("Step6");
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

                    return RedirectToAction("Step6");
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

                return RedirectToAction("Step6");
            }
            catch (JsonException)
            {
                TempData["Error"] = "The API returned an invalid response.";

                return RedirectToAction("Step6");
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

            // Validate SaleEndTime phải <= EventStartTime
            var eventInfoJsonR = HttpContext.Session.GetString("EventWizard_Info");
            if (!string.IsNullOrWhiteSpace(eventInfoJsonR))
            {
                var eventInfoR = JsonSerializer.Deserialize<EventInfoViewModel>(eventInfoJsonR);
                if (eventInfoR != null && eventInfoR.StartTime != default && ticket.SaleEndTime > eventInfoR.StartTime)
                    return Json(new { success = false, message = $"Thời gian kết thúc bán phải trước thời gian bắt đầu sự kiện ({eventInfoR.StartTime:dd/MM/yyyy HH:mm} UTC)." });
            }

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

            // Validate SaleEndTime phải <= EventStartTime
            var eventInfoJsonU = HttpContext.Session.GetString("EventWizard_Info");
            if (!string.IsNullOrWhiteSpace(eventInfoJsonU))
            {
                var eventInfoU = JsonSerializer.Deserialize<EventInfoViewModel>(eventInfoJsonU);
                if (eventInfoU != null && eventInfoU.StartTime != default && ticket.SaleEndTime > eventInfoU.StartTime)
                    return Json(new {
                        success = false,
                        field   = "SaleEndTime",
                        message = $"Thời gian kết thúc bán phải trước thời gian bắt đầu sự kiện ({eventInfoU.StartTime:dd/MM/yyyy HH:mm} UTC)."
                    });
            }

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

        private async Task<PaginationResponse<VenueResponse>> LoadVenuesAsync(HttpClient client, int page = 1, int pageSize = 6, string? search = null)
        {
            var url = $"api/Venue/venues?CurrentPage={page}&PageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await client.GetFromJsonAsync<
                ApiResponseModel<PaginationResponse<VenueResponse>>>(url);

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

        /// <summary>
        /// Cố gắng xóa Event Draft khi publish thất bại (rollback).
        /// Không throw exception — failure ở đây chỉ log, không block UI.
        /// </summary>
        private Task TryDeleteDraftEventAsync(HttpClient client, Guid eventId)
        {
            try
            {
                return client.DeleteAsync($"api/Events/{eventId}");
            }
            catch
            {
                // Best-effort rollback — bỏ qua nếu không xóa được
                return Task.CompletedTask;
            }
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
            List<CreateTicketTypeRequest> ticketTypes,
            DateTime eventStartTime)
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
                    return (false, $"Loại vé '{ticket.Name}': số lượng không hợp lệ.");

                if (ticket.Price < 0)
                    return (false, $"Loại vé '{ticket.Name}': giá không hợp lệ.");

                if (ticket.SaleStartTime == default)
                    return (false, $"Loại vé '{ticket.Name}': chưa thiết lập thời gian bắt đầu bán.");

                if (ticket.SaleEndTime == default)
                    return (false, $"Loại vé '{ticket.Name}': chưa thiết lập thời gian kết thúc bán.");

                if (ticket.SaleStartTime >= ticket.SaleEndTime)
                    return (false, $"Loại vé '{ticket.Name}': thời gian kết thúc bán phải sau thời gian bắt đầu bán.");

                // Rule từ backend: SaleEndTime phải <= EventStartTime
                if (ticket.SaleEndTime > eventStartTime)
                    return (false,
                        $"Loại vé '{ticket.Name}': thời gian kết thúc bán " +
                        $"({ticket.SaleEndTime:dd/MM/yyyy HH:mm} UTC) " +
                        $"phải trước thời gian bắt đầu sự kiện " +
                        $"({eventStartTime:dd/MM/yyyy HH:mm} UTC).");
            }

            return (true, string.Empty);
        }
    }
}
