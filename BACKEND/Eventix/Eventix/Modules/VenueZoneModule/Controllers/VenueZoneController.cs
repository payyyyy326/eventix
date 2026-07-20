using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.VenueZoneModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.VenueZone;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.VenueZoneModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class VenueZoneController : BaseApiController
    {
        private readonly IVenueZoneService _venueZoneService;

        public VenueZoneController(IVenueZoneService venueZoneService)
        {
            _venueZoneService = venueZoneService;
        }

        [HttpGet("venue/{venueId}")]
        public async Task<ActionResult<ApiResponseModel<List<VenueZoneResponse>>>> GetZonesByVenue(Guid venueId)
        {
            var result = await _venueZoneService.GetZonesByVenueAsync(venueId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        [HttpPost("venue/{venueId}")]
        public async Task<ActionResult<ApiResponseModel<VenueZoneResponse>>> CreateZone(
            Guid venueId,
            [FromBody] CreateVenueZoneRequest request)
        {
            var result = await _venueZoneService.CreateZoneAsync(venueId, request);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        [HttpPut("{zoneId}")]
        public async Task<ActionResult<ApiResponseModel<VenueZoneResponse>>> UpdateZone(
            Guid zoneId,
            [FromBody] UpdateVenueZoneRequest request)
        {
            var result = await _venueZoneService.UpdateZoneAsync(zoneId, request);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        [HttpGet("venue/{venueId}/seat-import-status")]
        public async Task<ActionResult<ApiResponseModel<List<SeatImportStatusResponse>>>> GetSeatImportStatus(Guid venueId)
        {
            var result = await _venueZoneService.GetSeatImportStatusAsync(venueId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        // GET api/VenueZone/event/{eventId}/zone-capacity
        [HttpGet("event/{eventId}/zone-capacity")]
        public async Task<ActionResult<ApiResponseModel<List<ZoneAvailableCapacityResponse>>>> GetZoneAvailableCapacity(Guid eventId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueZoneService.GetZoneAvailableCapacityAsync(eventId, userId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }
    }
}