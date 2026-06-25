using Eventix.Share.Common.Models;

namespace Eventix.Share.Event
{
    public class FilterEventRequest : PaginationRequest<EventResponse>
    {

        public Guid? CategoryId { get; set; }

        public Guid? VenueId { get; set; }

        public string? Search { get; set; } = null!;
        public DateTime? FromDate { get; set; }

        public string? SortBy { get; set; }

        public DateTime? ToDate { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string? Status { get; set; } = null!;

        public bool? IsFeatured { get; set; }
    }
}
