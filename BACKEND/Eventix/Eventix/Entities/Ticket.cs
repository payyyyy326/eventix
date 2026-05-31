using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", Name = "IX_Tickets_EventId")]
[Index("UserId", Name = "IX_Tickets_UserId")]
[Index("QrToken", Name = "UQ__Tickets__517D24C53EB953E9", IsUnique = true)]
[Index("TicketCode", Name = "UQ__Tickets__598CF7A39622F696", IsUnique = true)]
public partial class Ticket
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid TicketTypeId { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public Guid? SeatId { get; set; }

    [StringLength(100)]
    public string TicketCode { get; set; } = null!;

    [StringLength(255)]
    public string QrToken { get; set; } = null!;

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    [InverseProperty("Ticket")]
    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    [ForeignKey("EventId")]
    [InverseProperty("Tickets")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("Tickets")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("SeatId")]
    [InverseProperty("Tickets")]
    public virtual Seat? Seat { get; set; }

    [ForeignKey("TicketTypeId")]
    [InverseProperty("Tickets")]
    public virtual TicketType TicketType { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Tickets")]
    public virtual User User { get; set; } = null!;
}
