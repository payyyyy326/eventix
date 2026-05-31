using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class OrderItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPrice { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("OrderItems")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("OrderItems")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("SeatId")]
    [InverseProperty("OrderItems")]
    public virtual Seat? Seat { get; set; }

    [ForeignKey("TicketTypeId")]
    [InverseProperty("OrderItems")]
    public virtual TicketType TicketType { get; set; } = null!;
}
