using Eventix.Share.Venue;
using Eventix.Share.VenueZone;

namespace Eventix.Web.Models.EventWizard
{
    public class EventSeatAssignmentViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<SeatImportStatusResponse> SeatStatuses { get; set; } = new();

        public IFormFile? ExcelFile { get; set; }

        public bool CanContinue => SeatStatuses.All(x => x.Completed);
    }
}