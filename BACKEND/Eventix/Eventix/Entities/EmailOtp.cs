using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("Email", "Purpose", Name = "IX_EmailOtps_Email_Purpose")]
public partial class EmailOtp
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(10)]
    public string OtpCode { get; set; } = null!;

    [StringLength(50)]
    public string Purpose { get; set; } = null!;

    public bool IsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmailOtps")]
    public virtual User User { get; set; } = null!;
}
