using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class Venue
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public int Capacity { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("Venues")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("Venue")]
    public virtual ICollection<EventVenue> EventVenues { get; set; } = new List<EventVenue>();

    [InverseProperty("Venue")]
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
