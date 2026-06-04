using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = new List<EmailOtp>();

    public virtual ICollection<Event> EventCreatedByNavigations { get; set; } = new List<Event>();

    public virtual ICollection<Event> EventUpdatedByNavigations { get; set; } = new List<Event>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<OrganizerProfile> OrganizerProfileApprovedByNavigations { get; set; } = new List<OrganizerProfile>();

    public virtual OrganizerProfile? OrganizerProfileUser { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RefundRequest> RefundRequestReviewedByNavigations { get; set; } = new List<RefundRequest>();

    public virtual ICollection<RefundRequest> RefundRequestUsers { get; set; } = new List<RefundRequest>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<UserEventInteraction> UserEventInteractions { get; set; } = new List<UserEventInteraction>();

    public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();

    public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
