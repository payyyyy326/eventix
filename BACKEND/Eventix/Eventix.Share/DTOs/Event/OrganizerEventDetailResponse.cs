namespace Eventix.Share.Event
{
    public class OrganizerEventDetailResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public string? Summary { get; set; }
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        public string? BannerUrl { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }

        public string CategoryName { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueCity { get; set; } = string.Empty;

        public int TicketTypeCount { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsSold { get; set; }
        public int TicketsReserved { get; set; }
        public int TicketsRemaining { get; set; }

        public decimal Revenue { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}