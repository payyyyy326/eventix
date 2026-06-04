namespace Eventix.Entities;

public partial class EventAitag
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string Tag { get; set; } = null!;

    public decimal Confidence { get; set; }
}
