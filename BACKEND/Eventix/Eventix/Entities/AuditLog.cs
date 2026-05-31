using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("EntityType", "EntityId", Name = "IX_AuditLogs_Entity")]
public partial class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(255)]
    public string Action { get; set; } = null!;

    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [StringLength(100)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
}
