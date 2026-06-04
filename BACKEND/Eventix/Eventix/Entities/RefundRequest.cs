using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class RefundRequest
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public string? Reason { get; set; }

    public decimal RefundAmount { get; set; }

    public string RefundType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
