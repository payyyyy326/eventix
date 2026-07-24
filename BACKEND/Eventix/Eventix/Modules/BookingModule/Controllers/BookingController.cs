using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.BookingModule.Interfaces;
using Eventix.Share.Booking;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.BookingModule.Controllers;

[Route("api/bookings")]
[Authorize]
public class BookingController : BaseApiController
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseModel<List<BookingResponse>>>> CreateBooking(
        [FromBody] CreateBookingRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _bookingService.CreateBookingAsync(request, userId);
        return SuccessResponse(SystemSuccess.BOOKING_CREATED, result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponseModel<PaginationResponse<BookingResponse>>>>
        GetMyBookings([FromQuery] PaginationRequest<BookingResponse> request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _bookingService.GetMyBookingsAsync(request, userId);
        return SuccessResponse(SystemSuccess.BOOKINGS_RETRIEVED, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseModel<object>>> CancelBooking(Guid id)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _bookingService.CancelBookingAsync(id, userId);
        return SuccessResponse(SystemSuccess.BOOKING_CANCELLED);
    }
}
