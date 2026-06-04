namespace Eventix.Entities;

public partial class EventSeatStatus
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid SeatId { get; set; }

    public Guid TicketTypeId { get; set; }

    public string Status { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;

    public virtual TicketType TicketType { get; set; } = null!;
}
