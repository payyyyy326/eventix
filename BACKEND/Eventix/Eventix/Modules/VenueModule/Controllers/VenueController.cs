using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.VenueModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.SeatMap;
using Eventix.Share.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.VenueModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class VenueController : BaseApiController
    {
        private readonly IVenueService _venueService;

        public VenueController(IVenueService venueService)
        {
            _venueService = venueService;

        }

        [AllowAnonymous]
        //GET: api/venues
        [HttpGet("venues")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<VenueResponse>>>> GetAllVenues([FromQuery] PaginationRequest<VenueResponse> request)
        {
            var venues = await _venueService.GetAllVenuesAsync(request);
            return SuccessResponse(SystemSuccess.VENUES_RETRIEVED, venues);
        }

        //GET: api/venue/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseModel<VenueResponse>>> GetVenueById(Guid id)
        {
            var venue = await _venueService.GetVenueByIdAsync(id);
            return SuccessResponse(SystemSuccess.VENUE_RETRIEVED, venue);
        }

        //POST: api/venue
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseModel<VenueResponse>>> CreateVenue([FromBody] CreateVenueRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var venue = await _venueService.CreateVenueAsync(request, userId);
            return SuccessResponse(SystemSuccess.VENUE_CREATED, venue);
        }

        //PUT: api/venue/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponseModel<VenueResponse>>> UpdateVenue(Guid id, [FromBody] UpdateVenueRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var venue = await _venueService.UpdateVenueAsync(id, request, userId);
            return SuccessResponse(SystemSuccess.VENUE_UPDATED, venue);
        }

        [HttpGet("{venueId}/seat-map")]
        public async Task<ActionResult<ApiResponseModel<List<VenueSectionLayoutResponse>>>> GetSeatMap(Guid venueId)
        {
            var result = await _venueService.GetSeatMapAsync(venueId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        [HttpPut("{venueId}/seat-map")]
        public async Task<ActionResult<ApiResponseModel<List<VenueSectionLayoutResponse>>>> SaveSeatMap(
            Guid venueId,
            [FromBody] List<VenueSectionLayoutRequest> request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _venueService.SaveSeatMapAsync(venueId, request, userId);

            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

    }
}
