using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class CouponUsage
{
    [Key]
    public Guid Id { get; set; }

    public Guid CouponId { get; set; }

    public Guid UserId { get; set; }

    public Guid OrderId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; }

    public DateTime UsedAt { get; set; }

    [ForeignKey("CouponId")]
    [InverseProperty("CouponUsages")]
    public virtual Coupon Coupon { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("CouponUsages")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CouponUsages")]
    public virtual User User { get; set; } = null!;
}
