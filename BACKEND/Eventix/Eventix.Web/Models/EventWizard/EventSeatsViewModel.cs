using Eventix.Share.Seat;
using Eventix.Share.Venue;

namespace Eventix.Web.Models.EventWizard
{
    public class EventSeatsViewModel
    {
        public Guid VenueId { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<SeatSectionResponse> Sections { get; set; } = new();

        public int TotalSeats => Sections.Sum(x => x.SeatCount);

        public int TotalSections => Sections.Count;

        public IFormFile? ExcelFile { get; set; }
    }
}