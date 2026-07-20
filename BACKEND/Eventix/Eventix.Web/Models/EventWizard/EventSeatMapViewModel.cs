using Eventix.Share.SeatMap;
using Eventix.Share.Venue;
using Eventix.Share.VenueZone;

namespace Eventix.Web.Models.EventWizard
{
    public class EventSeatMapViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<VenueZoneResponse> Zones { get; set; } = new();

        public List<VenueSectionLayoutResponse> Layouts { get; set; } = new();
    }
}