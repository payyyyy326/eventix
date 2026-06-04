using System;
using System.Collections.Generic;

namespace Eventix.Entities;

public partial class UserEventInteraction
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid EventId { get; set; }

    public string InteractionType { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
