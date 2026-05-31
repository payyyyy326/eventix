using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", Name = "IX_TicketTypes_EventId")]
public partial class TicketType
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime SaleStartTime { get; set; }

    public DateTime SaleEndTime { get; set; }

    public bool IsSeatRequired { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("TicketType")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("EventId")]
    [InverseProperty("TicketTypes")]
    public virtual Event Event { get; set; } = null!;

    [InverseProperty("TicketType")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("TicketType")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [InverseProperty("TicketType")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
