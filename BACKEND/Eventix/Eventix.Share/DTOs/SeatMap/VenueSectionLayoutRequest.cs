namespace Eventix.Share.SeatMap
{
    public class VenueSectionLayoutRequest
    {
        public string Section { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Color { get; set; } = "#60A5FA";
    }
}