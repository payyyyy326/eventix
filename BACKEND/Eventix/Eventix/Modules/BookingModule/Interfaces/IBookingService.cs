using Eventix.Share.Booking;
using Eventix.Share.Common.Models;

namespace Eventix.Modules.BookingModule.Interfaces;

public interface IBookingService
{
    Task<List<BookingResponse>> CreateBookingAsync(
        CreateBookingRequest request,
        Guid userId);
    Task<PaginationResponse<BookingResponse>> GetMyBookingsAsync(
        PaginationRequest<BookingResponse> request,
        Guid userId);
    Task CancelBookingAsync(Guid bookingId, Guid userId);
}
