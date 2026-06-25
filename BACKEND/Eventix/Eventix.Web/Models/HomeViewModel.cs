using Eventix.Share.Category;
using Eventix.Share.Event;

namespace Eventix.Web.Models
{
    public class HomeViewModel
    {
        public List<CategoryResponse> Categories { get; set; } = new();
        public List<EventResponse> FeaturedEvents { get; set; } = new();
        public List<EventResponse> UpcomingEvents { get; set; } = new();
        public List<EventResponse> TrendingEvents { get; set; } = new();
        public List<EventResponse> Events { get; set; } = new();
    }
}
