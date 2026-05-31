using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("VenueId", "Section", "Row", "Number", Name = "UQ_Seats", IsUnique = true)]
public partial class Seat
{
    [Key]
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    [StringLength(100)]
    public string? Section { get; set; }

    [StringLength(50)]
    public string? Row { get; set; }

    [StringLength(50)]
    public string Number { get; set; } = null!;

    [Column("XPosition", TypeName = "decimal(10, 2)")]
    public decimal? Xposition { get; set; }

    [Column("YPosition", TypeName = "decimal(10, 2)")]
    public decimal? Yposition { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [InverseProperty("Seat")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [InverseProperty("Seat")]
    public virtual ICollection<EventSeatStatus> EventSeatStatuses { get; set; } = new List<EventSeatStatus>();

    [InverseProperty("Seat")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Seat")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [InverseProperty("Seat")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    [ForeignKey("VenueId")]
    [InverseProperty("Seats")]
    public virtual Venue Venue { get; set; } = null!;
}
