using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;

namespace Eventix.Modules.OrganizerModule.Interfaces
{
    public interface IOrganizerProfileService
    {
        Task<OrganizerProfileResponse> CreateAsync(CreateOrganizerProfileRequest request, Guid userId);
        Task<OrganizerProfileResponse> GetMyProfileAsync(Guid userId);
        Task<OrganizerProfileResponse> ApproveAsync(Guid organizerProfileId, Guid adminId);
        Task<OrganizerProfileResponse> RejectAsync(Guid organizerProfileId, Guid adminId);
        Task<PaginationResponse<OrganizerProfileResponse>> GetAllAsync(string status, PaginationRequest<OrganizerProfileResponse> request);
        Task<PaginationResponse<OrganizerEventResponse>> GetEventsByOrganizerAsync(Guid organizerId, PaginationRequest<OrganizerEventResponse> request);
        Task<OrganizerEventDetailResponse> GetOrganizerEventDetailAsync(Guid userId, Guid eventId);
        Task<List<string>> GetEventSectionsAsync(Guid userId, Guid eventId);
    }
}
