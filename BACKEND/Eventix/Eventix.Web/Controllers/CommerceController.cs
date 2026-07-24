using System.Net.Http.Headers;
using Eventix.Share.Commerce;
using Eventix.Share.Booking;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Web.Controllers;

public class CommerceController : Controller
{
    private readonly IHttpClientFactory _factory;
    public CommerceController(IHttpClientFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<IActionResult> Bookings()
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<
            ApiResponseModel<PaginationResponse<BookingResponse>>>(
            "api/bookings/my?CurrentPage=1&PageSize=100");
        return View(result?.Data?.DataList ?? []);
    }

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
                ? "Đã hủy giữ vé."
                : "Không thể hủy giữ vé.");
        return RedirectToAction("Bookings");
    }

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
        return RedirectToAction("Tickets");
    }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<ApiResponseModel<List<OrderResponse>>>("api/orders/my");
        return View(result?.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Tickets()
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<ApiResponseModel<List<TicketResponse>>>("api/tickets/my");
        return View(result?.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Ticket(Guid id)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var result = await client.GetFromJsonAsync<ApiResponseModel<TicketResponse>>($"api/tickets/{id}");
        return result?.Data == null ? NotFound() : View(result.Data);
    }

    [HttpGet]
    public IActionResult CheckIn(Guid? eventId) =>
        View(new CheckInRequest { EventId = eventId ?? Guid.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInRequest request)
    {
        var client = AuthorizedClient();
        if (client == null) return RedirectToAction("Login", "Auth");
        var response = await client.PostAsJsonAsync("api/checkin/scan", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<CheckInResponse>>();
        if (!response.IsSuccessStatusCode || result?.Data == null)
        {
            ModelState.AddModelError("", result?.Message ?? "Không thể check-in vé.");
            return View(request);
        }
        ViewBag.Result = result.Data;
        return View(request);
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
