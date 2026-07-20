using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventVenueViewModel
    {
        public Guid? SelectedVenueId { get; set; }

        public List<VenueResponse> Venues { get; set; } = new();

        public CreateVenueRequest NewVenue { get; set; } = new();

        public string Mode { get; set; } = "select";
    }
}