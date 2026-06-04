using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class Reservation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public Guid? OrderId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User User { get; set; } = null!;
}
