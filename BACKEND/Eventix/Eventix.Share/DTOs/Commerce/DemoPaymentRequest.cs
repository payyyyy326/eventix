using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Commerce;

public class DemoPaymentRequest
{
    [Required]
    public Guid OrderId { get; set; }
}
