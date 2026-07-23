namespace Eventix.Share.Commerce;

public class TicketResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public DateTime EventStartTime { get; set; }
    public string VenueName { get; set; } = "";
    public string TicketTypeName { get; set; } = "";
    public string? SeatLabel { get; set; }
    public string TicketCode { get; set; } = "";
    public string QrToken { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime IssuedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
