using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.CommerceModule.Controllers;

[Route("api/checkin")]
[Authorize(Roles = "Organizer,Admin")]
public class CheckInController : BaseApiController
{
    private readonly ICommerceService _service;
    public CheckInController(ICommerceService service) => _service = service;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole(SystemConstants.RoleConstants.ADMIN);

    [HttpPost("scan")]
    public async Task<ActionResult<ApiResponseModel<CheckInResponse>>> Scan(
        CheckInRequest request) =>
        SuccessResponse(SystemSuccess.SUCCESS,
            await _service.CheckInAsync(request, UserId, IsAdmin));

    [HttpGet("event/{eventId:guid}/stats")]
    public async Task<ActionResult<ApiResponseModel<CheckInStatsResponse>>> Stats(Guid eventId) =>
        SuccessResponse(SystemSuccess.SUCCESS,
            await _service.GetCheckInStatsAsync(eventId, UserId, IsAdmin));
}
