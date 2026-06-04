namespace Eventix.Entities;

public partial class RefundPolicy
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public bool IsRefundAllowed { get; set; }

    public int RefundBeforeHours { get; set; }

    public decimal RefundPercent { get; set; }

    public string? Description { get; set; }
}
