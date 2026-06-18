namespace Eventix.Share.Event
{
    public class EventResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } = null!;
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;

        public Guid VenueId { get; set; }
        public string VenueName { get; set; } = null!;
        public string? VenueCity { get; set; }
    }
}
