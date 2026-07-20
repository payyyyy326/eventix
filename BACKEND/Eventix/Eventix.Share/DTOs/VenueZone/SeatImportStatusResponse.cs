namespace Eventix.Share.VenueZone
{
    public class SeatImportStatusResponse
    {
        public Guid VenueZoneId { get; set; }

        public string ZoneName { get; set; } = string.Empty;

        public bool HasSeats { get; set; }

        public int Capacity { get; set; }

        public int ImportedSeats { get; set; }

        public bool Completed { get; set; }
    }
}