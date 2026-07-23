namespace Eventix.Share.Event;

public class BookingSeatResponse
{
    public Guid SeatId { get; set; }
    public Guid TicketTypeId { get; set; }
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? XPosition { get; set; }
    public decimal? YPosition { get; set; }
}
