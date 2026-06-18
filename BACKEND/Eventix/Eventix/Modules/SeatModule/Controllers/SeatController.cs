using Eventix.Common.Constants.SystemData;
using Eventix.Modules.SeatModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.Seat;
using Microsoft.AspNetCore.Mvc;
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

        //GET: api/Seat/venue/{venueId}
        [HttpGet("venue/{venueId}")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<SeatResponse>>>> GetSeatsByVenueId(Guid venueId, [FromQuery] PaginationRequest<SeatResponse> request)
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

        //[Authorize(Policy = SystemConstants.RoleConstants.ADMIN)]
        [HttpPost("{venueId}/import-excel")]
        public async Task<ActionResult<ApiResponseModel<ImportSeatResult>>> ImportSeatByExcel(
        Guid venueId,
        [FromForm] ImportSeatsRequest request)
        {
            var result = await _seatService.ImportSeatByExcelAsync(venueId, request);

            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }


    }
}
