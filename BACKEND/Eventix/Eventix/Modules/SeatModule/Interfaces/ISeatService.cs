using Eventix.Share.Common.Models;
using Eventix.Share.Seat;
using Eventix.Share.SeatMap;

namespace Eventix.Modules.SeatModule.Interfaces
{
    public interface ISeatService
    {
        byte[] GenerateSeatTemplateExcel();
        Task<ImportSeatResult> ImportSeatByExcelAsync(Guid venueId, ImportSeatsRequest request);
        Task<PaginationResponse<SeatResponse>> GetSeatsByVenueAsync(Guid venueId, PaginationRequest<SeatResponse> request);
        Task<List<SeatSectionResponse>> GetSectionsByVenueAsync(Guid venueId);
        Task<ImportSeatResult> GenerateSeatsAsync(Guid venueId, GenerateSeatsRequest request);

        /// <summary>
        /// Lấy trạng thái generate seat cho từng TicketType có IsSeatRequired = true trong event.
        /// Thay thế cho SeatImportStatus theo VenueZone.
        /// </summary>
        Task<List<TicketTypeSeatStatusResponse>> GetSeatAssignmentStatusByEventAsync(Guid eventId);

        /// <summary>
        /// Lấy seatmap (danh sách VenueSectionLayout) của một event cụ thể.
        /// Mỗi block tương ứng với một TicketType.
        /// </summary>
        Task<List<VenueSectionLayoutResponse>> GetSeatMapByEventAsync(Guid eventId);

        /// <summary>
        /// Lấy danh sách ghế (kèm trạng thái) thuộc một TicketType trong một event cụ thể.
        /// Chỉ dùng khi TicketType.IsSeatRequired = true.
        /// </summary>
        Task<List<SeatWithStatusResponse>> GetSeatsByTicketTypeAsync(Guid eventId, Guid ticketTypeId);
    }
}
