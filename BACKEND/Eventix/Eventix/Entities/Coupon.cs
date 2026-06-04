namespace Eventix.Entities;

public partial class Coupon
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Scope { get; set; } = null!;

    public Guid? EventId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
