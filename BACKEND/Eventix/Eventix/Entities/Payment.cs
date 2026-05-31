using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("OrderId", Name = "IX_Payments_OrderId")]
public partial class Payment
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(50)]
    public string Gateway { get; set; } = null!;

    [StringLength(255)]
    public string? TransactionCode { get; set; }

    [StringLength(255)]
    public string? GatewayTransactionId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = null!;

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public string? PaymentUrl { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Payments")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Payments")]
    public virtual User User { get; set; } = null!;
}
