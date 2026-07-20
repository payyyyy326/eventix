namespace Eventix.Share.Seat
{
    public class GenerateSeatsRequest
    {
        public Guid VenueZoneId { get; set; }

        public string StartRow { get; set; } = string.Empty;

        public string EndRow { get; set; } = string.Empty;

        public int StartNumber { get; set; }

        public int EndNumber { get; set; }

        public decimal StartX { get; set; }

        public decimal StartY { get; set; }

        public decimal GapX { get; set; }

        public decimal GapY { get; set; }

        public bool OverrideExisting { get; set; }
    }
}