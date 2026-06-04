namespace Eventix.Entities;

public partial class EventImage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int SortOrder { get; set; }
}
