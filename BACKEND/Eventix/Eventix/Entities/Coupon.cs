using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("Code", Name = "UQ__Coupons__A25C5AA744D7FCDF", IsUnique = true)]
public partial class Coupon
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string DiscountType { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [StringLength(50)]
    public string Scope { get; set; } = null!;

    public Guid? EventId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Coupon")]
    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    [ForeignKey("EventId")]
    [InverseProperty("Coupons")]
    public virtual Event? Event { get; set; }

    [InverseProperty("Coupon")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
