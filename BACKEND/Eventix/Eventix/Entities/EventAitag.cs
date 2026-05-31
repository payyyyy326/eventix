using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Table("EventAITags")]
public partial class EventAitag
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    [StringLength(100)]
    public string Tag { get; set; } = null!;

    [Column(TypeName = "decimal(5, 4)")]
    public decimal Confidence { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("EventAitags")]
    public virtual Event Event { get; set; } = null!;
}
