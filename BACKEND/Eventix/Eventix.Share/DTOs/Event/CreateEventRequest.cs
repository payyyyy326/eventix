namespace Eventix.Share.Event
{
    public class CreateEventRequest
    {
        public Guid CategoryId { get; set; }

        public Guid VenueId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? Summary { get; set; }

        public string? ImageUrl { get; set; }

        public string? BannerUrl { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = null!;

        public bool IsFeatured { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
