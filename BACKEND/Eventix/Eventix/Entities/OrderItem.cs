using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public virtual Order Order { get; set; } = null!;
}
