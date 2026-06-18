using Eventix.Share.Common.Models;
using Eventix.Share.Seat;

namespace Eventix.Modules.SeatModule.Interfaces
{
    public interface ISeatService
    {
        public byte[] GenerateSeatTemplateExcel();
        public Task<ImportSeatResult> ImportSeatByExcelAsync(Guid venueId, ImportSeatsRequest request);
        public Task<PaginationResponse<SeatResponse>> GetSeatsByVenueAsync(Guid venueId, PaginationRequest<SeatResponse> request);
    }
}
