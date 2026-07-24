using Eventix.Share.Common.Models;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventVenueViewModel
    {
        public Guid? SelectedVenueId { get; set; }

        // Dùng PaginationResponse thay vì List thuần
        public PaginationResponse<VenueResponse> VenuePage { get; set; } = new();

        // Shortcut để lấy danh sách venue trang hiện tại
        public List<VenueResponse> Venues => VenuePage.DataList;

        public CreateVenueRequest NewVenue { get; set; } = new();

        public string Mode { get; set; } = "select";
    }
}
