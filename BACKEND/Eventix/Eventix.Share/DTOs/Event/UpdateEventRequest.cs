namespace Eventix.Share.Event
{
    public class UpdateEventRequest
    {
        public Guid OrganizerId { get; set; }

        public Guid CategoryId { get; set; }

        public Guid VenueId { get; set; }

        public string Title { get; set; } = null!;

        public string Slug { get; set; } = null!;

        public string? Description { get; set; }

        public string? Summary { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = null!;

        public int ViewCount { get; set; }

        public bool IsFeatured { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
