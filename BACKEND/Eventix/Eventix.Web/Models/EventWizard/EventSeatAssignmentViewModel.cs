using Eventix.Share.TicketType;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventSeatAssignmentViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new();

        // Seats will be auto-generated when the event is published
        public bool CanContinue => TicketTypes.Any();
    }
}