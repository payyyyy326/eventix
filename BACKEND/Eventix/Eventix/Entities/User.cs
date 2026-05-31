using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("Email", Name = "UQ__Users__A9D105342ED2E389", IsUnique = true)]
public partial class User
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(255)]
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("User")]
    public virtual Cart? Cart { get; set; }

    [InverseProperty("CheckedInByNavigation")]
    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    [InverseProperty("User")]
    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    [InverseProperty("User")]
    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = new List<EmailOtp>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("ApprovedByNavigation")]
    public virtual ICollection<OrganizerProfile> OrganizerProfileApprovedByNavigations { get; set; } = new List<OrganizerProfile>();

    [InverseProperty("User")]
    public virtual OrganizerProfile? OrganizerProfileUser { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("ReviewedByNavigation")]
    public virtual ICollection<RefundRequest> RefundRequestReviewedByNavigations { get; set; } = new List<RefundRequest>();

    [InverseProperty("User")]
    public virtual ICollection<RefundRequest> RefundRequestUsers { get; set; } = new List<RefundRequest>();

    [InverseProperty("User")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [InverseProperty("User")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("User")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    [InverseProperty("User")]
    public virtual ICollection<UserEventInteraction> UserEventInteractions { get; set; } = new List<UserEventInteraction>();

    [InverseProperty("User")]
    public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
