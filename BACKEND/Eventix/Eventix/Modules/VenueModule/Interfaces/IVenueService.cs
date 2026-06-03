using Eventix.Common.Models;
using Eventix.Modules.VenueModule.DTOs;

namespace Eventix.Modules.VenueModule.Interfaces
{
    public interface IVenueService
    {
        Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request, Guid userId);
        Task<VenueResponse> GetVenueByIdAsync(Guid id);
        Task<PaginationResponse<VenueResponse>> GetAllVenuesAsync(PaginationRequest<VenueResponse> request);
        Task<VenueResponse> UpdateVenueAsync(Guid id, UpdateVenueRequest request, Guid userId);
        Task DeleteVenueAsync(Guid id);
    }
}
