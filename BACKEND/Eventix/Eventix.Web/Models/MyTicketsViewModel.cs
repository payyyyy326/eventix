using Eventix.Share.Booking;
using Eventix.Share.Commerce;

namespace Eventix.Web.Models;

public class MyTicketsViewModel
{
    public List<BookingResponse> Bookings { get; set; } = [];
    public List<TicketResponse> Tickets { get; set; } = [];
    public int TotalTicketCount { get; set; }
    public int FilteredTicketCount { get; set; }
    public int TicketPage { get; set; } = 1;
    public int TicketPageSize { get; set; } = 10;
    public int TicketTotalPages { get; set; } = 1;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> TicketStatuses { get; set; } = [];
}