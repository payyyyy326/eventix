using Eventix.Common.Models;
using Eventix.Modules.EventModule.DTOs;

namespace Eventix.Modules.EventModule.Interfaces
{
    public interface IEventService
    {
        Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId);
        Task<EventResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid userId);
        Task<bool> DeleteEventAsync(Guid eventId, Guid organizerId);
        Task<EventResponse> GetEventByIdAsync(Guid eventId);
        Task<PaginationResponse<EventResponse>> GetEventsByOrganizerAsync(Guid organizerId, PaginationRequest<EventResponse> request);
        Task<PaginationResponse<EventResponse>> GetEventsAsync(FIlterEventRequest request);

        Task<EventResponse> UpLoadBannerAsync(Guid eventId, string bannerUrl, Guid organizerId);
        Task<EventResponse> UpLoadImageAsync(Guid eventId, string imageUrl, Guid organizerId);
    }
}
