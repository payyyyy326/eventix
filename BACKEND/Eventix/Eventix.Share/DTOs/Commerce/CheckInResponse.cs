namespace Eventix.Share.Commerce;

public class CheckInResponse
{
    public Guid TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string TicketTypeName { get; set; } = "";
    public string? SeatLabel { get; set; }
    public DateTime CheckedInAt { get; set; }
}

public class CheckInStatsResponse
{
    public Guid EventId { get; set; }
    public int TotalTickets { get; set; }
    public int CheckedInTickets { get; set; }
    public int RemainingTickets { get; set; }
}
