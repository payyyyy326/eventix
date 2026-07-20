using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.EventModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
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

        // GET: api/events
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<EventResponse>>>> GetEvents([FromQuery] FilterEventRequest request)
        {
            var events = await _eventService.GetEventsAsync(request);
            return SuccessResponse(SystemSuccess.EVENTS_RETRIEVED, events);
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<EventDetailResponse>>> GetEventById(Guid id)
        {
            var eventResponse = await _eventService.GetEventByIdAsync(id);
            return SuccessResponse(SystemSuccess.EVENT_RETRIEVED, eventResponse);
        }

        // GET: api/events/{id}/booking
        [HttpGet("{id}/booking")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseModel<EventBookingResponse>>> GetEventBooking(Guid id)
        {
            var eventResponse = await _eventService.GetEventBookingAsync(id);
            return SuccessResponse(SystemSuccess.EVENT_RETRIEVED, eventResponse);
        }

        //POST: api/events/create
        [HttpPost("create")]
        [Authorize(Policy = SystemConstants.RoleConstants.ORGANIZER)]
        public async Task<ActionResult<ApiResponseModel<EventDetailResponse>>> CreateEvent(
            [FromBody] CreateEventRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.CreateEventAsync(request, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_CREATED, eventResponse);
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponseModel<EventDetailResponse>>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpdateEventAsync(id, request, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_UPDATED, eventResponse);
        }

        // PUT: api/events/{id}/upload-banner
        [HttpPut("{id}/upload-banner")]
        public async Task<ActionResult<ApiResponseModel<EventDetailResponse>>> UploadEventBanner(Guid id, [FromBody] string bannerUrl)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpLoadBannerAsync(id, bannerUrl, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_BANNER_UPLOADED, eventResponse);
        }

        // PUT: api/events/{id}/upload-image
        [HttpPut("{id}/upload-image")]
        public async Task<ActionResult<ApiResponseModel<EventDetailResponse>>> UploadEventImage(Guid id, [FromBody] string imageUrl)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var eventResponse = await _eventService.UpLoadImageAsync(id, imageUrl, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_IMAGE_UPLOADED, eventResponse);
        }

        [HttpPost("{eventId:guid}/publish")]
        public async Task<ActionResult<ApiResponseModel<EventResponse>>> PublishEvent(Guid eventId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var result = await _eventService.PublishEventAsync(
                eventId,
                userId);

            return SuccessResponse(SystemSuccess.EVENT_PUBLISHED, result);
        }
    }
}