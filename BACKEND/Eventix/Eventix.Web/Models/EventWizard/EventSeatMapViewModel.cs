using Eventix.Share.SeatMap;
using Eventix.Share.TicketType;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventSeatMapViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        /// <summary>
        /// Ticket types for the current wizard session (from session storage).
        /// Used to build the map legend and section labels.
        /// </summary>
        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new();

        /// <summary>
        /// Section layout blocks (one per ticket type) to display on the map canvas.
        /// Pre-populated if this venue had a prior layout saved.
        /// </summary>
        public List<VenueSectionLayoutResponse> Layouts { get; set; } = new();
    }
}