namespace Eventix.Share.Event
{
    public class OrganizerEventResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } = null!;
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }

        public string CategoryName { get; set; } = null!;
        public string VenueName { get; set; } = null!;

        public int TotalTicketTypes { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
