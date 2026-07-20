namespace Eventix.Share.SeatMap
{
    public class VenueSectionLayoutResponse
    {
        public Guid Id { get; set; }
        public Guid VenueId { get; set; }

        public string Section { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Color { get; set; } = string.Empty;
    }
}