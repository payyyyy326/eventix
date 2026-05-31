using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

public partial class Notification
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = null!;

    [StringLength(255)]
    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
