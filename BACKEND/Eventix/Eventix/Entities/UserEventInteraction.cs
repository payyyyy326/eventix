using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class UserEventInteraction
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid EventId { get; set; }

    [StringLength(50)]
    public string InteractionType { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("UserEventInteractions")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserEventInteractions")]
    public virtual User? User { get; set; }
}
