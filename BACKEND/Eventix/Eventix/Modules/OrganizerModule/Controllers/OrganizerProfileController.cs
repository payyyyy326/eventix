using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.OrganizerModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Organizer;
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

        public OrganizerProfileController(IOrganizerProfileService organizerProfileService)
        {
            _organizerProfileService = organizerProfileService;
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

        //POST: api/Organizer/create
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseModel<OrganizerProfileResponse>>> CreateOrganizerProfile([FromBody] CreateOrganizerProfileRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _organizerProfileService.CreateAsync(request, userId);
            return SuccessResponse(SystemSuccess.ORGANIZER_CREATED, profile);
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
    }
}
