using Eventix.Share.TicketType;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventTicketTypesViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new();

        public CreateTicketTypeRequest NewTicketType { get; set; } = new();

        /// <summary>
        /// Thời gian bắt đầu sự kiện — dùng để giới hạn SaleEndTime tối đa.
        /// </summary>
        public DateTime? EventStartTime { get; set; }
    }
}
