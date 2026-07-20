namespace Eventix.Entities;

public partial class TicketType
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public string? Section { get; set; }

    public DateTime SaleStartTime { get; set; }

    public DateTime SaleEndTime { get; set; }

    public bool IsSeatRequired { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string Status { get; set; } = null!;

    public Guid? VenueZoneId { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<EventSeatStatus> EventSeatStatuses { get; set; } = new List<EventSeatStatus>();

    public virtual VenueZone? VenueZone { get; set; }
}
