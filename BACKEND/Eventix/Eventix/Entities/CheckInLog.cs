using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", Name = "IX_CheckInLogs_EventId")]
public partial class CheckInLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Guid EventId { get; set; }

    public Guid CheckedInBy { get; set; }

    public DateTime CheckInTime { get; set; }

    [StringLength(50)]
    public string Method { get; set; } = null!;

    public string? Note { get; set; }

    [ForeignKey("CheckedInBy")]
    [InverseProperty("CheckInLogs")]
    public virtual User CheckedInByNavigation { get; set; } = null!;

    [ForeignKey("EventId")]
    [InverseProperty("CheckInLogs")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("CheckInLogs")]
    public virtual Ticket Ticket { get; set; } = null!;
}
