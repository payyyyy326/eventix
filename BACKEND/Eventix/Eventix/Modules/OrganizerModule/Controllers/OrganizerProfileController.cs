using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.OrganizerModule.Interfaces;
using Eventix.Modules.TicketTypeModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;
using Eventix.Share.TicketType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.OrganizerModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class OrganizerProfileController : BaseApiController
    {
        private readonly IOrganizerProfileService _organizerProfileService;
        private readonly ITicketTypeService _ticketTypeService;

        public OrganizerProfileController(IOrganizerProfileService organizerProfileService, ITicketTypeService ticketTypeService)
        {
            _organizerProfileService = organizerProfileService;
            _ticketTypeService = ticketTypeService;
        }

        //GET: api/organizer-profiles
        [HttpGet("organizer-profiles")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<OrganizerProfileResponse>>>> GetOrganizerProfiles([FromQuery] string? status, [FromQuery] PaginationRequest<OrganizerProfileResponse> request)
        {
            var profiles = await _organizerProfileService.GetAllAsync(status, request);
            return SuccessResponse(SystemSuccess.ORGANIZERS_RETRIEVED, profiles);
        }

        //GET: api/organizer-detail
        [HttpGet("organizer-detail")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> GetOrganizerDetail()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _organizerProfileService.GetMyProfileAsync(userId);
            return SuccessResponse(SystemSuccess.ORGANIZER_RETRIEVED, profile);
        }

        [HttpGet("events")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<OrganizerEventResponse>>>> GetEvents([FromQuery] PaginationRequest<OrganizerEventResponse> request)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response =
                await _organizerProfileService.GetEventsByOrganizerAsync(
                    userId,
                    request);

            return SuccessResponse(
                SystemSuccess.EVENTS_RETRIEVED,
                response);
        }

        //POST: api/Organizer/create
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> CreateOrganizerProfile([FromBody] CreateOrganizerProfileRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _organizerProfileService.CreateAsync(request, userId);
            return SuccessResponse(SystemSuccess.ORGANIZER_CREATED, profile);
        }

        //GET: api/Organizer/events/{id}
        [HttpGet("events/{id}")]
        public async Task<ActionResult<ApiResponseModel<OrganizerEventDetailResponse>>> GetEventDetail(Guid id)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _organizerProfileService.GetOrganizerEventDetailAsync(userId, id);

            return SuccessResponse(
                SystemSuccess.EVENT_RETRIEVED,
                response);
        }

        //GET: api/Organizer/events/{id}/ticket-types
        [HttpGet("events/{eventId}/ticket-types")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<TicketTypeResponse>>>> GetTicketTypes(
    Guid eventId,
    [FromQuery] PaginationRequest<TicketTypeResponse> request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ticketTypeService.GetTicketTypesByOrganizerEventAsync(userId, eventId, request);

            return SuccessResponse(SystemSuccess.TICKET_TYPES_RETRIEVED, response);
        }

        [HttpPut("detail")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> UpdateOrganizerDetail([FromBody] UpdateOrganizerProfileRequest request)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var profile =
                await _organizerProfileService.UpdateMyProfileAsync(
                    userId,
                    request);

            return SuccessResponse(SystemSuccess.ORGANIZER_UPDATED, profile);
        }

        //PATCH : api/organizer/{id}/approve
        [Authorize(Roles = SystemConstants.RoleConstants.ADMIN)]
        [HttpPatch("{id}/approve")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> Approve(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _organizerProfileService.ApproveAsync(id, adminId);

            return SuccessResponse(SystemSuccess.ORGANIZER_APPROVED, response);
        }

        //PATCH : api/organizer/{id}/rejected
        [Authorize(Roles = SystemConstants.RoleConstants.ADMIN)]
        [HttpPatch("{id}/reject")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> Reject(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _organizerProfileService.RejectAsync(id, adminId);
            return SuccessResponse(SystemSuccess.ORGANIZER_REJECTED, response);
        }

        [HttpGet("events/{eventId}/sections")]
        public async Task<ActionResult<ApiResponseModel<List<string>>>> GetSections(Guid eventId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _organizerProfileService.GetEventSectionsAsync(userId, eventId);

            return SuccessResponse(SystemSuccess.SEATS_RETRIEVED, response);
        }

        [HttpPost("events/{eventId}/ticket-types")]
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> CreateTicketType(
    Guid eventId,
    [FromBody] CreateTicketTypeRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ticketTypeService.CreateTicketTypeAsync(eventId, request, userId);

            return SuccessResponse(SystemSuccess.TICKET_TYPE_CREATED, response);
        }

        [HttpGet("ticket-types/{id}")]
        //GET: api/ticket-types/{id}
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> GetTicketTypeById(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ticketTypeService.GetTicketTypeByIdForOrganizerAsync(userId, id);

            return SuccessResponse(SystemSuccess.TICKET_TYPE_RETRIEVED, response);
        }

        [HttpPut("ticket-types/{id}")]
        //PUT: api/TicketType/ticket-types/{id}
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> UpdateTicketTypes([FromBody] UpdateTicketTypeRequest request, Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _ticketTypeService.UpdateTicketTypeAsync(id, request, userId);
            return SuccessResponse(SystemSuccess.TICKET_TYPE_UPDATED, response);
        }

        [HttpPatch("ticket-types/{id}/deactivate")]
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> DeactivateTicketType(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ticketTypeService.DeactivateTicketTypeAsync(id, userId);

            return SuccessResponse(SystemSuccess.TICKET_TYPE_UPDATED, response);
        }
    }
}
