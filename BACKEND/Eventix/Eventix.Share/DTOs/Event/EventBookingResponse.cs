using Eventix.Share.TicketType;
using Eventix.Share.Venue;

namespace Eventix.Share.Event
{
    public class EventBookingResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public string? BannerUrl { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public VenueResponse Venue { get; set; } = null!;

        public List<TicketTypeResponse> TicketTypes { get; set; } = new();
    }
}
