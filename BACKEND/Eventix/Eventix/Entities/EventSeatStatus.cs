using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", "SeatId", Name = "UQ_EventSeat", IsUnique = true)]
public partial class EventSeatStatus
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid SeatId { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [ForeignKey("EventId")]
    [InverseProperty("EventSeatStatuses")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("SeatId")]
    [InverseProperty("EventSeatStatuses")]
    public virtual Seat Seat { get; set; } = null!;
}
