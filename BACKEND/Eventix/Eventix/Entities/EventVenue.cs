using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EventId", Name = "UQ_EventVenues_Event", IsUnique = true)]
public partial class EventVenue
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid VenueId { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("EventVenue")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("VenueId")]
    [InverseProperty("EventVenues")]
    public virtual Venue Venue { get; set; } = null!;
}
