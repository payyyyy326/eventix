using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class EventImage
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int SortOrder { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("EventImages")]
    public virtual Event Event { get; set; } = null!;
}
