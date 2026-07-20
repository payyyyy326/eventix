using Eventix.Share.Common.Models;
using Eventix.Share.SeatMap;
using Eventix.Share.Venue;

namespace Eventix.Modules.VenueModule.Interfaces
{
    public interface IVenueService
    {
        Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request, Guid userId);
        Task<VenueResponse> GetVenueByIdAsync(Guid id);
        Task<PaginationResponse<VenueResponse>> GetAllVenuesAsync(PaginationRequest<VenueResponse> request);
        Task<VenueResponse> UpdateVenueAsync(Guid id, UpdateVenueRequest request, Guid userId);
        Task DeleteVenueAsync(Guid id);
        Task<List<VenueSectionLayoutResponse>> GetSeatMapAsync(Guid venueId);

        Task<List<VenueSectionLayoutResponse>> SaveSeatMapAsync(Guid venueId, List<VenueSectionLayoutRequest> request, Guid userId);
    }
}
