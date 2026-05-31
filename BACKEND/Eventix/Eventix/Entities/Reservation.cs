using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("ExpiresAt", "Status", Name = "IX_Reservations_ExpiresAt")]
public partial class Reservation
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid? SeatId { get; set; }

    public Guid? OrderId { get; set; }

    public int Quantity { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("Reservations")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("Reservations")]
    public virtual Order? Order { get; set; }

    [ForeignKey("SeatId")]
    [InverseProperty("Reservations")]
    public virtual Seat? Seat { get; set; }

    [ForeignKey("TicketTypeId")]
    [InverseProperty("Reservations")]
    public virtual TicketType TicketType { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reservations")]
    public virtual User User { get; set; } = null!;
}
