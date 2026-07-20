namespace Eventix.Entities;

public partial class VenueSectionLayout
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public string Section { get; set; } = null!;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string Color { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? VenueZoneId { get; set; }

    public virtual Venue Venue { get; set; } = null!;

    public virtual VenueZone? VenueZone { get; set; }
}
