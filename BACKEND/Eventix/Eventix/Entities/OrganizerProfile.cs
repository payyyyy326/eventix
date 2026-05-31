using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("UserId", Name = "UQ__Organize__1788CC4DF8873FCF", IsUnique = true)]
public partial class OrganizerProfile
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string OrganizationName { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(255)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("ApprovedBy")]
    [InverseProperty("OrganizerProfileApprovedByNavigations")]
    public virtual User? ApprovedByNavigation { get; set; }

    [InverseProperty("Organizer")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [ForeignKey("UserId")]
    [InverseProperty("OrganizerProfileUser")]
    public virtual User User { get; set; } = null!;
}
