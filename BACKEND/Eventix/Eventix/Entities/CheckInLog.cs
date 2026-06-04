namespace Eventix.Entities;

public partial class CheckInLog
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Guid EventId { get; set; }

    public Guid CheckedInBy { get; set; }

    public DateTime CheckInTime { get; set; }

    public string Method { get; set; } = null!;

    public string? Note { get; set; }

    public virtual User CheckedInByNavigation { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
