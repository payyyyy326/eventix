using Eventix.Share.Booking;
using Eventix.Share.Commerce;

namespace Eventix.Web.Models;

public class MyTicketsViewModel
{
    public List<BookingResponse> Bookings { get; set; } = [];
    public List<TicketResponse> Tickets { get; set; } = [];
}
