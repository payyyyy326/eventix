using Eventix.Share.TicketType;
using Eventix.Share.Venue;
using Eventix.Share.VenueZone;

namespace Eventix.Web.Models.EventWizard
{
    public class EventReviewViewModel
    {
        public EventInfoViewModel? EventInfo { get; set; }

        public VenueResponse? Venue { get; set; }

        public List<VenueZoneResponse> Zones { get; set; } = new();

        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new();

        public List<SeatReviewItemViewModel> SeatStatuses { get; set; } = new();

        public bool HasSavedMap { get; set; }

        public int TotalZoneCapacity =>
            Zones.Sum(x => x.Capacity);

        public int TotalTicketQuantity =>
            TicketTypes.Sum(x => x.Quantity);

        public bool HasZones =>
            Zones.Count > 0;

        public bool HasTicketTypes =>
            TicketTypes.Count > 0;

        public bool AllSeatedZonesCompleted =>
            SeatStatuses.All(x =>
                !x.HasSeats || x.Completed);

        public bool CapacityIsValid =>
            Venue == null ||
            TotalZoneCapacity <= Venue.Capacity;

        public bool CanPublish =>
            EventInfo != null &&
            Venue != null &&
            HasZones &&
            HasTicketTypes &&
            AllSeatedZonesCompleted &&
            CapacityIsValid &&
            HasSavedMap;
    }

    public class SeatReviewItemViewModel
    {
        public Guid VenueZoneId { get; set; }

        public string ZoneName { get; set; } = string.Empty;

        public bool HasSeats { get; set; }

        public int Capacity { get; set; }

        public int SeatCount { get; set; }

        public bool Completed { get; set; }
    }
}