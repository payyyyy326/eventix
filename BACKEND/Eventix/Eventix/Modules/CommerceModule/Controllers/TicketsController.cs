using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Models;
using Eventix.Share.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.CommerceModule.Controllers;

[Route("api/tickets")]
[Authorize]
public class TicketsController : BaseApiController
{
    private readonly ICommerceService _service;
    public TicketsController(ICommerceService service) => _service = service;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponseModel<List<TicketResponse>>>> My() =>
        SuccessResponse(SystemSuccess.SUCCESS, await _service.GetMyTicketsAsync(UserId));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseModel<TicketResponse>>> Detail(Guid id) =>
        SuccessResponse(SystemSuccess.SUCCESS, await _service.GetTicketAsync(id, UserId));

    [HttpGet("event/{eventId:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponseModel<List<TicketResponse>>>> EventTickets(
        Guid eventId) =>
        SuccessResponse(SystemSuccess.SUCCESS,
            await _service.GetEventTicketsAsync(
                eventId,
                UserId,
                User.IsInRole(SystemConstants.RoleConstants.ADMIN)));
}
