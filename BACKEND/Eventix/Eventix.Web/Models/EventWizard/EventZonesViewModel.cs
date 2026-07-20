using Eventix.Share.Venue;
using Eventix.Share.VenueZone;

namespace Eventix.Web.Models.EventWizard
{
    public class EventZonesViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<VenueZoneResponse> Zones { get; set; } = new();

        public CreateVenueZoneRequest NewZone { get; set; } = new()
        {
            HasSeats = true,
            Color = "#60A5FA",
            SortOrder = 1,
            Capacity = 0
        };
    }
}