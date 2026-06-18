namespace Eventix.Share.Event
{
    public class AdminEventResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string CategoryName { get; set; } = null!;
        public string VenueName { get; set; } = null!;
        public string? OrganizerName { get; set; }

        public bool IsFeatured { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
