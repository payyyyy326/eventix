namespace Eventix.Entities;

public partial class VenueZone
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public string Name { get; set; } = null!;

    public bool HasSeats { get; set; }

    public int Capacity { get; set; }

    public string Color { get; set; } = null!;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    public virtual Venue Venue { get; set; } = null!;

    public virtual ICollection<VenueSectionLayout> VenueSectionLayouts { get; set; } = new List<VenueSectionLayout>();
}
