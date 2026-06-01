using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eventix.Entities;

[Index("Slug", Name = "UQ__Categori__BC7B5FB61865F06C", IsUnique = true)]
public partial class Category
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
