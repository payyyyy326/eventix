using Eventix.Share.Category;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;

namespace Eventix.Web.Models
{
    public class EventViewModel
    {
        public FilterEventRequest Filter { get; set; } = new();
        public PaginationResponse<EventResponse> Events { get; set; } = new();
        public List<CategoryResponse> Categories { get; set; } = [];

    }
}
