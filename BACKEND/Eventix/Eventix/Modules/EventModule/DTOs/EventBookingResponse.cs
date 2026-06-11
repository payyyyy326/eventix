using Eventix.Modules.TicketTypeModule.DTOs;
using Eventix.Modules.VenueModule.DTOs;

namespace Eventix.Modules.EventModule.DTOs
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
