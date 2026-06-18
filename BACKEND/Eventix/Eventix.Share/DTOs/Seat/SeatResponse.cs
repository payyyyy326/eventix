namespace Eventix.Share.Seat
{
    public class SeatResponse
    {
        public Guid Id { get; set; }

        public Guid VenueId { get; set; }

        public string? Section { get; set; }

        public string? Row { get; set; }

        public string Number { get; set; } = null!;

        public decimal? Xposition { get; set; }

        public decimal? Yposition { get; set; }

        public string Status { get; set; } = null!;
    }
}
