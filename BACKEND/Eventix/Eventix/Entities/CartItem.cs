namespace Eventix.Entities;

public partial class CartItem
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Cart Cart { get; set; } = null!;
}
