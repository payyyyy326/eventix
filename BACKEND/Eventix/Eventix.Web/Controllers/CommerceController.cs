using System.Net.Http.Headers;
using Eventix.Share.Commerce;
using Eventix.Share.Booking;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Web.Controllers;

public class CommerceController : Controller
{
    private readonly IHttpClientFactory _factory;
    public CommerceController(IHttpClientFactory factory) => _factory = factory;

    // ─── Trang gộp Vé đang giữ + Vé đã mua ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> MyTickets(
        string tab = "bookings", string? search = null, string? status = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        int page = 1, int pageSize = 10)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        ViewBag.ActiveTab = tab;

        var bookingsTask = client.GetAsync("api/bookings/my?CurrentPage=1&PageSize=100");
        var ticketsTask = client.GetAsync("api/tickets/my");
        await Task.WhenAll(bookingsTask, ticketsTask);

        if (bookingsTask.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ticketsTask.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            ClearExpiredSession();
            TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Auth");
        }

        var bookingsResult = bookingsTask.Result.IsSuccessStatusCode
            ? await bookingsTask.Result.Content.ReadFromJsonAsync<ApiResponseModel<PaginationResponse<BookingResponse>>>()
            : null;
        var ticketsResult = ticketsTask.Result.IsSuccessStatusCode
            ? await ticketsTask.Result.Content.ReadFromJsonAsync<ApiResponseModel<List<TicketResponse>>>()
            : null;
        var allTickets = ticketsResult?.Data ?? [];        var filteredTickets = allTickets.AsEnumerable();
        search = search?.Trim();
        status = status?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
            filteredTickets = filteredTickets.Where(ticket =>
                ticket.EventTitle.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                ticket.TicketCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                ticket.TicketTypeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                ticket.VenueName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (ticket.SeatLabel?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(status))
            filteredTickets = filteredTickets.Where(ticket =>
                string.Equals(ticket.Status, status, StringComparison.OrdinalIgnoreCase));
        if (fromDate.HasValue)
            filteredTickets = filteredTickets.Where(ticket => ticket.EventStartTime.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            filteredTickets = filteredTickets.Where(ticket => ticket.EventStartTime.Date <= toDate.Value.Date);

        pageSize = new[] { 5, 10, 20, 50 }.Contains(pageSize) ? pageSize : 10;
        var orderedTickets = filteredTickets.OrderByDescending(ticket => ticket.EventStartTime).ToList();
        var totalFilteredTickets = orderedTickets.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalFilteredTickets / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new MyTicketsViewModel
        {
            Bookings = bookingsResult?.Data?.DataList ?? [],
            Tickets = orderedTickets.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalTicketCount = allTickets.Count,
            FilteredTicketCount = totalFilteredTickets,
            TicketPage = page,
            TicketPageSize = pageSize,
            TicketTotalPages = totalPages,
            Search = search,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            TicketStatuses = allTickets.Select(ticket => ticket.Status)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList()
        });
    }
    [HttpGet]
    public IActionResult Bookings() =>
        RedirectToAction("MyTickets", new { tab = "bookings" });

    [HttpGet]
    public IActionResult Tickets() =>
        RedirectToAction("MyTickets", new { tab = "tickets" });

    // ─── Huỷ giữ vé ─────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");

        var response = await client.DeleteAsync($"api/bookings/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<object>>();

        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            result?.Message ?? (response.IsSuccessStatusCode
                ? "Đã huỷ giữ vé."
                : "Không thể huỷ giữ vé.");

        return RedirectToAction("MyTickets", new { tab = "bookings" });
    }

    // ─── Checkout ────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Checkout(List<Guid> reservationIds)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");

        var response = await client.PostAsJsonAsync("api/orders",
            new CreateOrderRequest { ReservationIds = reservationIds });
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<OrderResponse>>();

        if (!response.IsSuccessStatusCode || result?.Data == null)
        {
            TempData["Error"] = result?.Message ?? "Không thể tạo đơn hàng.";
            return RedirectToAction("Orders");
        }

        return View(result.Data);
    }

    // ─── Thanh toán Demo ─────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(Guid orderId)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");

        var response = await client.PostAsJsonAsync("api/payments/demo/complete",
            new DemoPaymentRequest { OrderId = orderId });
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<PaymentResponse>>();

        if (!response.IsSuccessStatusCode || result?.Data == null)
        {
            TempData["Error"] = result?.Message ?? "Thanh toán không thành công.";
            return RedirectToAction("Orders");
        }

        TempData["Success"] = "Thanh toán thành công. Vé điện tử đã được phát hành.";
        return RedirectToAction("MyTickets", new { tab = "tickets" });
    }

    // ─── Đơn hàng ────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<ApiResponseModel<List<OrderResponse>>>("api/orders/my");
        return View(result?.Data ?? []);
    }

    // ─── Xem vé điện tử đơn lẻ ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Ticket(Guid id)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<ApiResponseModel<TicketResponse>>($"api/tickets/{id}");
        return result?.Data == null ? NotFound() : View(result.Data);
    }

    // ─── Check-in (standalone page — vẫn giữ để backward compat) ────────────
    [HttpGet]
    public IActionResult CheckIn(Guid? eventId) =>
        View(new CheckInRequest { EventId = eventId ?? Guid.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInRequest request)
    {
        ViewBag.CheckInMode = "manual";
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");

        var response = await client.PostAsJsonAsync("api/checkin/scan", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<CheckInResponse>>();

        // Nếu có returnEventId, redirect về ManageEvent tab check-in
        if (Request.Form.TryGetValue("returnEventId", out var returnId)
            && Guid.TryParse(returnId, out var eventId))
        {
            if (!response.IsSuccessStatusCode || result?.Data == null)
            {
                TempData["Error"] = result?.Message ?? "Không thể check-in vé.";
            }
            else
            {
                TempData["Success"] = $"Check-in thành công: {result.Data.CustomerName} — {result.Data.TicketCode}";
            }
            return RedirectToAction("ManageEvent", "Organizer",
                new { id = eventId, tab = "checkin" });
        }

        if (!response.IsSuccessStatusCode || result?.Data == null)
        {
            ModelState.AddModelError("", result?.Message ?? "Không thể check-in vé.");
            return View(request);
        }

        ViewBag.Result = result.Data;
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInImage(Guid eventId, IFormFile? qrImage)
    {
        ViewBag.CheckInMode = "image";
        var request = new CheckInRequest { EventId = eventId };
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");

        if (eventId == Guid.Empty)
        {
            ModelState.AddModelError("", "Vui lòng nhập Event ID.");
            return View("CheckIn", request);
        }
        if (qrImage == null || qrImage.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng chọn ảnh chứa mã QR.");
            return View("CheckIn", request);
        }
        if (qrImage.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("", "Ảnh QR không được vượt quá 5 MB.");
            return View("CheckIn", request);
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(eventId.ToString()), "eventId");
        await using var imageStream = qrImage.OpenReadStream();
        using var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(qrImage.ContentType)
                ? "application/octet-stream"
                : qrImage.ContentType);
        form.Add(imageContent, "qrImage", qrImage.FileName);

        var response = await client.PostAsync("api/checkin/scan-image", form);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponseModel<CheckInResponse>>();

        // Nếu có returnEventId, redirect về ManageEvent tab check-in
        if (Request.Form.TryGetValue("returnEventId", out var returnId)
            && Guid.TryParse(returnId, out var retEventId))
        {
            if (!response.IsSuccessStatusCode || result?.Data == null)
            {
                TempData["Error"] = result?.Message ?? "Không thể đọc hoặc check-in QR từ ảnh.";
            }
            else
            {
                TempData["Success"] = $"Check-in thành công: {result.Data.CustomerName} — {result.Data.TicketCode}";
            }
            return RedirectToAction("ManageEvent", "Organizer",
                new { id = retEventId, tab = "checkin" });
        }

        if (!response.IsSuccessStatusCode || result?.Data == null)
        {
            ModelState.AddModelError("", result?.Message ?? "Không thể đọc hoặc check-in QR từ ảnh.");
            return View("CheckIn", request);
        }

        ViewBag.Result = result.Data;
        return View("CheckIn", request);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────
    private void ClearExpiredSession()
    {
        Response.Cookies.Delete(SystemConstants.CookieNames.Token);
        Response.Cookies.Delete(SystemConstants.CookieNames.RefreshToken);
        Response.Cookies.Delete(SystemConstants.CookieNames.UserName);
        Response.Cookies.Delete(SystemConstants.CookieNames.AvatarUrl);
        Response.Cookies.Delete(SystemConstants.CookieNames.Roles);
    }
    private HttpClient? AuthorizedClient()
    {
        var token = Request.Cookies[SystemConstants.CookieNames.Token];
        if (string.IsNullOrWhiteSpace(token)) return null;
        var client = _factory.CreateClient("Eventix");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
