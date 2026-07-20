namespace Eventix.Share.VenueZone
{
    public class UpdateVenueZoneRequest
    {
        public string Name { get; set; } = string.Empty;

        public bool HasSeats { get; set; }

        public int Capacity { get; set; }

        public string Color { get; set; } = "#60A5FA";

        public int SortOrder { get; set; }
    }
}