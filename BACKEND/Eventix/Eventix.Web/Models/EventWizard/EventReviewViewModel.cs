using Eventix.Share.TicketType;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventReviewViewModel
    {
        public EventInfoViewModel? EventInfo { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new();

        public int TotalTicketQuantity =>
            TicketTypes.Sum(x => x.Quantity);

        public bool HasTicketTypes =>
            TicketTypes.Count > 0;

        public bool CanPublish =>
            EventInfo != null &&
            Venue != null &&
            HasTicketTypes;
    }
}
