using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Controllers;
using Eventix.Modules.EventModule.DTOs;
using Eventix.Modules.EventModule.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.EventModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : BaseApiController
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        //GET: api/event/events
        [HttpGet("events")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<EventResponse>>>> GetEvents([FromQuery] FIlterEventRequest request)
        {
            var events = await _eventService.GetEventsAsync(request);
            return SuccessResponse(SystemSuccess.EVENTS_RETRIEVED, events);
        }

        //GET: api/event/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> GetEventById(Guid id)
        {
            var eventResponse = await _eventService.GetEventByIdAsync(id);
            return SuccessResponse(SystemSuccess.EVENT_RETRIEVED, eventResponse);
        }

        //POST: api/event/create
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> CreateEvent([FromBody] CreateEventRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.CreateEventAsync(request, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_CREATED, eventResponse);
        }

        //PUT: api/event/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpdateEventAsync(id, request, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_UPDATED, eventResponse);
        }

        //PUT: api/event/{id}/upload-banner
        [HttpPut("{id}/upload-banner")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> UploadEventBanner(Guid id, [FromBody] string bannerUrl)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpLoadBannerAsync(id, bannerUrl, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_BANNER_UPLOADED, eventResponse);
        }

        //PUT: api/event/{id}/upload-image
        [HttpPut("{id}/upload-image")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> UploadEventImage(Guid id, [FromBody] string imageUrl)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpLoadImageAsync(id, imageUrl, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_IMAGE_UPLOADED, eventResponse);
        }
    }
}