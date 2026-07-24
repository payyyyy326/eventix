using Eventix.Common.Constants.SystemData;
using Eventix.Modules.SeatModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.Seat;
using Eventix.Share.SeatMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TIMORA.Controllers;

namespace Eventix.Modules.SeatModule.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatController : BaseApiController
    {
        private readonly ISeatService _seatService;

        public SeatController(ISeatService seatService)
        {
            _seatService = seatService;
        }

        // GET: api/Seat/venue/{venueId}
        [HttpGet("venue/{venueId}")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<SeatResponse>>>> GetSeatsByVenueId(
            Guid venueId,
            [FromQuery] PaginationRequest<SeatResponse> request)
        {
            var seats = await _seatService.GetSeatsByVenueAsync(venueId, request);
            return SuccessResponse(SystemSuccess.SEATS_RETRIEVED, seats);
        }

        // GET: api/Seat/template
        [HttpGet("template")]
        public IActionResult DownloadSeatTemplate()
        {
            var fileBytes = _seatService.GenerateSeatTemplateExcel();
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "seat_import_template.xlsx"
            );
        }

        // POST: api/Seat/{venueId}/import-excel
        [HttpPost("{venueId}/import-excel")]
        public async Task<ActionResult<ApiResponseModel<ImportSeatResult>>> ImportSeatByExcel(
            Guid venueId,
            [FromForm] ImportSeatsRequest request)
        {
            var result = await _seatService.ImportSeatByExcelAsync(venueId, request);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        // GET: api/Seat/venue/{venueId}/sections
        [HttpGet("venue/{venueId}/sections")]
        public async Task<ActionResult<ApiResponseModel<List<SeatSectionResponse>>>> GetSectionsByVenue(Guid venueId)
        {
            var result = await _seatService.GetSectionsByVenueAsync(venueId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        // POST: api/Seat/venue/{venueId}/generate
        [HttpPost("venue/{venueId}/generate")]
        public async Task<ActionResult<ApiResponseModel<ImportSeatResult>>> GenerateSeats(
            Guid venueId,
            [FromBody] GenerateSeatsRequest request)
        {
            var result = await _seatService.GenerateSeatsAsync(venueId, request);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>
        /// GET: api/Seat/event/{eventId}/assignment-status
        /// Trả về trạng thái generate seat theo từng TicketType (thay vì VenueZone).
        /// Dùng cho Step 5 của wizard.
        /// </summary>
        [HttpGet("event/{eventId}/assignment-status")]
        [Authorize]
        public async Task<ActionResult<ApiResponseModel<List<TicketTypeSeatStatusResponse>>>> GetSeatAssignmentStatus(Guid eventId)
        {
            var result = await _seatService.GetSeatAssignmentStatusByEventAsync(eventId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>
        /// GET: api/Seat/event/{eventId}/seatmap
        /// Lấy sơ đồ map (các block TicketType) của một event.
        /// Dùng cho cả buyer và organizer để hiển thị layout.
        /// </summary>
        [HttpGet("event/{eventId}/seatmap")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<List<VenueSectionLayoutResponse>>>> GetSeatMapByEvent(Guid eventId)
        {
            var result = await _seatService.GetSeatMapByEventAsync(eventId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>
        /// GET: api/Seat/event/{eventId}/tickettype/{ticketTypeId}/seats
        /// Lấy danh sách ghế + trạng thái của một TicketType trong event.
        /// Chỉ có giá trị khi TicketType.IsSeatRequired = true.
        /// </summary>
        [HttpGet("event/{eventId}/tickettype/{ticketTypeId}/seats")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<List<SeatWithStatusResponse>>>> GetSeatsByTicketType(
            Guid eventId,
            Guid ticketTypeId)
        {
            var result = await _seatService.GetSeatsByTicketTypeAsync(eventId, ticketTypeId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>
        /// GET: api/Seat/organizer/event/{eventId}/seatmap
        /// Organizer xem seatmap của event thuộc mình.
        /// Yêu cầu xác thực, trả về cùng dữ liệu như endpoint public nhưng có thể mở rộng sau.
        /// </summary>
        [HttpGet("organizer/event/{eventId}/seatmap")]
        [Authorize]
        public async Task<ActionResult<ApiResponseModel<List<VenueSectionLayoutResponse>>>> GetOrganizerEventSeatMap(Guid eventId)
        {
            var result = await _seatService.GetSeatMapByEventAsync(eventId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }
    }
}
