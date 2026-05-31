using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class RefundRequest
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public string? Reason { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RefundAmount { get; set; }

    [StringLength(50)]
    public string RefundType { get; set; } = null!;

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("RefundRequests")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("ReviewedBy")]
    [InverseProperty("RefundRequestReviewedByNavigations")]
    public virtual User? ReviewedByNavigation { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefundRequestUsers")]
    public virtual User User { get; set; } = null!;
}
