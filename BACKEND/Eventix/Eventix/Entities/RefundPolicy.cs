using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", Name = "UQ__RefundPo__7944C8112F6A04C1", IsUnique = true)]
public partial class RefundPolicy
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public bool IsRefundAllowed { get; set; }

    public int RefundBeforeHours { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RefundPercent { get; set; }

    public string? Description { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("RefundPolicy")]
    public virtual Event Event { get; set; } = null!;
}
