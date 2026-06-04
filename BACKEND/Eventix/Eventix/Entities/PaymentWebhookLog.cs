namespace Eventix.Entities;

public partial class PaymentWebhookLog
{
    public Guid Id { get; set; }

    public string Gateway { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public string? RawPayload { get; set; }

    public string? Signature { get; set; }

    public bool IsValid { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
