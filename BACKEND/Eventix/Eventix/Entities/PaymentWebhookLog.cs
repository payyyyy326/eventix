using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Eventix.Entities;

[Index("Gateway", "EventId", Name = "UQ_Webhook", IsUnique = true)]
public partial class PaymentWebhookLog
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(50)]
    public string Gateway { get; set; } = null!;

    [StringLength(255)]
    public string EventId { get; set; } = null!;

    [StringLength(255)]
    public string? TransactionCode { get; set; }

    public string? RawPayload { get; set; }

    public string? Signature { get; set; }

    public bool IsValid { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
