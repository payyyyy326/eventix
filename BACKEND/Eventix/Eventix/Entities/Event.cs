using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("CategoryId", Name = "IX_Events_CategoryId")]
[Index("City", Name = "IX_Events_City")]
[Index("Status", "StartTime", Name = "IX_Events_Status_StartTime")]
[Index("ViewCount", Name = "IX_Events_ViewCount")]
[Index("Slug", Name = "UQ__Events__BC7B5FB663463F80", IsUnique = true)]
public partial class Event
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizerId { get; set; }

    public Guid CategoryId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    [StringLength(255)]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Summary { get; set; }

    public string? ImageUrl { get; set; }

    public string? BannerUrl { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [StringLength(255)]
    public string? LocationName { get; set; }

    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [StringLength(50)]
    public string EventType { get; set; } = null!;

    public int Capacity { get; set; }

    public int ViewCount { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    [InverseProperty("Event")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Events")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("Event")]
    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    [InverseProperty("Event")]
    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    [InverseProperty("Event")]
    public virtual ICollection<EventAitag> EventAitags { get; set; } = new List<EventAitag>();

    [InverseProperty("Event")]
    public virtual ICollection<EventImage> EventImages { get; set; } = new List<EventImage>();

    [InverseProperty("Event")]
    public virtual ICollection<EventSeatStatus> EventSeatStatuses { get; set; } = new List<EventSeatStatus>();

    [InverseProperty("Event")]
    public virtual EventVenue? EventVenue { get; set; }

    [InverseProperty("Event")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("OrganizerId")]
    [InverseProperty("Events")]
    public virtual OrganizerProfile Organizer { get; set; } = null!;

    [InverseProperty("Event")]
    public virtual RefundPolicy? RefundPolicy { get; set; }

    [InverseProperty("Event")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [InverseProperty("Event")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("Event")]
    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    [InverseProperty("Event")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    [InverseProperty("Event")]
    public virtual ICollection<UserEventInteraction> UserEventInteractions { get; set; } = new List<UserEventInteraction>();
}
