using Eventix.Share.VenueZone;

namespace Eventix.Modules.VenueZoneModule.Interfaces
{
    public interface IVenueZoneService
    {
        Task<List<VenueZoneResponse>> GetZonesByVenueAsync(Guid venueId);

        Task<VenueZoneResponse> CreateZoneAsync(Guid venueId, CreateVenueZoneRequest request);

        Task<VenueZoneResponse> UpdateZoneAsync(Guid zoneId, UpdateVenueZoneRequest request);

        Task<List<SeatImportStatusResponse>> GetSeatImportStatusAsync(Guid venueId);

        /// <summary>
        /// Trả về số slot còn trống của từng zone cho một event cụ thể.
        /// Dùng khi organizer tạo loại vé để biết zone còn chứa được bao nhiêu.
        /// </summary>
        Task<List<ZoneAvailableCapacityResponse>> GetZoneAvailableCapacityAsync(Guid eventId, Guid userId);
    }
}