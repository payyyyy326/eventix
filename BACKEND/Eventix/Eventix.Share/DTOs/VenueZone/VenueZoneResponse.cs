namespace Eventix.Share.VenueZone
{
    public class VenueZoneResponse
    {
        public Guid Id { get; set; }

        public Guid VenueId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool HasSeats { get; set; }

        public int Capacity { get; set; }

        public string Color { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public int SeatCount { get; set; }
    }
}