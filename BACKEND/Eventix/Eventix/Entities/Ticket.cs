using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class Ticket
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public Guid? SeatId { get; set; }

    public string TicketCode { get; set; } = null!;

    public string QrToken { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    public virtual Order Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
