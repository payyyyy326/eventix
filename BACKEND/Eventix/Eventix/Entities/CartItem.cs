using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class CartItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid CartId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey("CartId")]
    [InverseProperty("CartItems")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("EventId")]
    [InverseProperty("CartItems")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("SeatId")]
    [InverseProperty("CartItems")]
    public virtual Seat? Seat { get; set; }

    [ForeignKey("TicketTypeId")]
    [InverseProperty("CartItems")]
    public virtual TicketType TicketType { get; set; } = null!;
}
