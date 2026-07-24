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

    /// <summary>
    /// FK tới VenueZone (legacy, vẫn giữ để tương thích).
    /// </summary>
    public Guid? VenueZoneId { get; set; }

    /// <summary>
    /// FK tới TicketType (luồng mới: map block theo loại vé).
    /// </summary>
    public Guid? TicketTypeId { get; set; }

    public virtual Venue Venue { get; set; } = null!;

    public virtual VenueZone? VenueZone { get; set; }

    public virtual TicketType? TicketType { get; set; }
}
