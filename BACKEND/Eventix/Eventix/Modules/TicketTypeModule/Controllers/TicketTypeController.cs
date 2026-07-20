using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.TicketTypeModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.TicketType;
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

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<TicketTypeResponse>>>> GetTicketTypes([FromQuery] Guid eventId, [FromQuery] PaginationRequest<TicketTypeResponse> request)
        {
            var reponse = await _ticketTypeService.GetTicketTypesByEventIdAsync(eventId, request);
            return SuccessResponse(SystemSuccess.TICKET_TYPES_RETRIEVED, reponse);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        //GET: api/ticketType/{id}
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> GetTicketTypeById([FromQuery] Guid id)
        {
            var response = await _ticketTypeService.GetTicketTypeByIdAsync(id);
            return SuccessResponse(SystemSuccess.TICKET_TYPE_RETRIEVED, response);
        }

        [HttpPut("update/{id}")]
        //PUT: api/TicketType/update
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> UpdateTicketTypes([FromBody] UpdateTicketTypeRequest request, [FromQuery] Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _ticketTypeService.UpdateTicketTypeAsync(id, request, userId);
            return SuccessResponse(SystemSuccess.TICKET_TYPE_UPDATED, response);
        }

        [HttpPost("event/{eventId}")]
        public async Task<ActionResult<ApiResponseModel<TicketTypeResponse>>> CreateTicketType(Guid eventId, [FromBody] CreateTicketTypeRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ticketTypeService.CreateTicketTypeAsync(eventId, request, userId);

            return SuccessResponse(SystemSuccess.TICKET_TYPE_CREATED, response);
        }
    }
}
