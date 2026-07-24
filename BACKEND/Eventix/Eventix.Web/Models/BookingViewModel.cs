using Eventix.Share.Booking;
using Eventix.Share.Event;

namespace Eventix.Web.Models;

public class BookingViewModel
{
    public EventBookingResponse Event { get; set; } = new();
    public CreateBookingRequest Request { get; set; } = new();
}
