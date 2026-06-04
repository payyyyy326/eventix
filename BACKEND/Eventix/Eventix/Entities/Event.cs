namespace Eventix.Entities;

public partial class Event
{
    public Guid Id { get; set; }

    public Guid OrganizerId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid VenueId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Summary { get; set; }

    public string? ImageUrl { get; set; }

    public string? BannerUrl { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Status { get; set; } = null!;

    public int ViewCount { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<EventSeatStatus> EventSeatStatuses { get; set; } = new List<EventSeatStatus>();

    public virtual OrganizerProfile Organizer { get; set; } = null!;

    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual Venue Venue { get; set; } = null!;
}
