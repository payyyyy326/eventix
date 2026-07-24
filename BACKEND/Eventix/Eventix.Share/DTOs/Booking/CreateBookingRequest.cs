using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Booking;

public class CreateBookingRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid TicketTypeId { get; set; }

    [MaxLength(10, ErrorMessage = "You can select at most 10 seats")]
    public List<Guid> SeatIds { get; set; } = [];

    [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
    public int Quantity { get; set; } = 1;
}
