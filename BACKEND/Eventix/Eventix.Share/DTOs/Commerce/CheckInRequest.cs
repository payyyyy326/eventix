using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Commerce;

public class CheckInRequest
{
    [Required]
    public string QrToken { get; set; } = "";

    [Required]
    public Guid EventId { get; set; }
}
