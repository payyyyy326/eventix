using Eventix.Common.Models;
using Eventix.Modules.OrganizerModule.DTOs;

namespace Eventix.Modules.OrganizerModule.Interfaces
{
    public interface IOrganizerProfileService
    {
        Task<OrganizerProfileResponse> CreateAsync(CreateOrganizerProfileRequest request, Guid userId);
        Task<OrganizerProfileResponse> GetMyProfileAsync(Guid userId);
        Task<OrganizerProfileResponse> ApproveAsync(Guid organizerProfileId, Guid adminId);
        Task<OrganizerProfileResponse> RejectAsync(Guid organizerProfileId, Guid adminId);
        Task<PaginationResponse<OrganizerProfileResponse>> GetAllAsync(string status, PaginationRequest<OrganizerProfileResponse> request);
    }
}
