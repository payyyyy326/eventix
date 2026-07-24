using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Commerce;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> ReservationIds { get; set; } = [];
}
