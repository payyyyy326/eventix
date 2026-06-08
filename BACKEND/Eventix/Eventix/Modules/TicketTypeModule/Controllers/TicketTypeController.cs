using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Controllers;
using Eventix.Modules.TicketTypeModule.DTOs;
using Eventix.Modules.TicketTypeModule.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.TicketTypeModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TicketTypeController : BaseApiController
    {
        private readonly ITicketTypeService _ticketTypeService;

        public TicketTypeController(ITicketTypeService ticketTypeService)
        {
            _ticketTypeService = ticketTypeService;
        }

        //GET: api/ticketType/gets
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<TicketTypeResponse>>>> GetTicketTypes([FromQuery] Guid eventId, [FromQuery] PaginationRequest<TicketTypeResponse> request)
        {
            var reponse = await _ticketTypeService.GetTicketTypesByEventIdAsync(eventId, request);
            return SuccessResponse(SystemSuccess.TICKET_TYPES_RETRIEVED, reponse);
        }

        //GET: api/ticketType/{id}
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> GetTicketTypeById([FromQuery] Guid ticketTypeId)
        {
            var response = await _ticketTypeService.GetTicketTypeByIdAsync(ticketTypeId);
            return SuccessResponse(SystemSuccess.TICKET_TYPE_RETRIEVED, response);
        }

        //POST: api/ticketType/create
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> CreateTicketType([FromBody] CreateTicketTypeRequest request, [FromQuery] Guid eventId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _ticketTypeService.CreateTicketTypeAsync(eventId, request, userId);
            return SuccessResponse(SystemSuccess.TICKET_TYPES_RETRIEVED, response);
        }

        //PUT: api/TicketType/update
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> UpdateTicketTypes([FromBody] UpdateTicketTypeRequest request, [FromQuery] Guid eventId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _ticketTypeService.UpdateTicketTypeAsync(eventId, request, userId);
            return SuccessResponse(SystemSuccess.TICKET_TYPE_UPDATED, response);
        }
    }
}
