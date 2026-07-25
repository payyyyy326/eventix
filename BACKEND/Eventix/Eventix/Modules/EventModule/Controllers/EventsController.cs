using Eventix.Common.Constants.SystemData;
using Eventix.Common.Settings;
using Eventix.Controllers;
using Eventix.Modules.EventModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Eventix.Modules.EventModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : BaseApiController
    {
        private readonly IEventService _eventService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApiSettings _apiSettings;

        public EventsController(
            IEventService eventService,
            IWebHostEnvironment environment,
            IOptions<ApiSettings> apiSettings)
        {
            _eventService = eventService;
            _environment  = environment;
            _apiSettings  = apiSettings.Value;
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

        // DELETE: api/events/{id}  — chỉ dùng để rollback wizard (xóa Draft event)
        [HttpDelete("{id}")]
        [Authorize(Policy = SystemConstants.RoleConstants.ORGANIZER)]
        public async Task<ActionResult<ApiResponseModel<bool>>> DeleteDraftEvent(Guid id)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _eventService.DeleteEventAsync(id, organizerId);
            return SuccessResponse(SystemSuccess.EVENT_DELETED, result);
        }

        // POST: api/events/upload-image
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseModel<UploadImageResponse>>> UploadEventImageFile(
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiResponseModel<object>("File is required.", false, null));

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new ApiResponseModel<object>("Only image files (jpg, png, webp, gif) are allowed.", false, null));

            const long maxSize = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxSize)
                return BadRequest(new ApiResponseModel<object>("File size must not exceed 5 MB.", false, null));

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var folder = Path.Combine(webRoot, "uploads", "events");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relativeUrl = $"/uploads/events/{fileName}";
            var absoluteUrl = $"{_apiSettings.BaseUrl}{relativeUrl}";

            return SuccessResponse(
                SystemSuccess.EVENT_IMAGE_UPLOADED,
                new UploadImageResponse { Url = absoluteUrl, RelativeUrl = relativeUrl });
        }
    }

    /// <summary>Response trả về sau khi upload ảnh thành công.</summary>
    public sealed class UploadImageResponse
    {
        /// <summary>URL tuyệt đối, dùng để hiển thị preview trong trình duyệt.</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>URL tương đối, lưu vào ImageUrl / BannerUrl của event.</summary>
        public string RelativeUrl { get; set; } = string.Empty;
    }
}