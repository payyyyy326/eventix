using Eventix.Share.Booking;
using Eventix.Share.Event;
using Eventix.Share.SeatMap;

namespace Eventix.Web.Models;

public class BookingViewModel
{
    public EventBookingResponse Event { get; set; } = new();
    public CreateBookingRequest Request { get; set; } = new();
    public List<VenueSectionLayoutResponse> SeatMapLayouts { get; set; } = [];
}
