using Eventix.Share.Common.Models;
using Eventix.Share.Event;

namespace Eventix.Modules.EventModule.Interfaces
{
    public interface IEventService
    {
        Task<EventDetailResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId);
        Task<EventDetailResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid userId);
        Task<bool> DeleteEventAsync(Guid eventId, Guid organizerId);
        Task<EventDetailResponse> GetEventByIdAsync(Guid eventId);
        Task<PaginationResponse<OrganizerEventResponse>> GetEventsByOrganizerAsync(Guid organizerId, PaginationRequest<OrganizerEventResponse> request);
        Task<PaginationResponse<EventResponse>> GetEventsAsync(FIlterEventRequest request);
        Task<EventBookingResponse> GetEventBookingAsync(Guid eventId);
        Task<EventDetailResponse> UpLoadBannerAsync(Guid eventId, string bannerUrl, Guid organizerId);
        Task<EventDetailResponse> UpLoadImageAsync(Guid eventId, string imageUrl, Guid organizerId);
    }
}
