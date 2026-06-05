using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Common.Models;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.EventModule.DTOs;
using Eventix.Modules.EventModule.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Eventix.Common.Constants.SystemConstants;

namespace Eventix.Modules.EventModule.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(x => x.UserId == organizerId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            if (organizer.Status != OrganizerStatus.APPROVED)
            {
                throw new BadRequestException(
                    SystemError.ORGANIZER_NOT_APPROVED);
            }

            var categoryExists = await _context.Categories.AnyAsync(x => x.Id == request.CategoryId);

            if (!categoryExists)
            {
                throw new BadRequestException(
                    SystemError.CATEGORY_NOT_FOUND);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newEvent = new Event
                {
                    Id = Guid.NewGuid(),
                    OrganizerId = organizer.Id,
                    CategoryId = request.CategoryId,
                    VenueId = request.VenueId,
                    Title = request.Title,
                    Slug = request.Slug,
                    Description = request.Description,
                    Summary = request.Summary,
                    ImageUrl = request.ImageUrl,
                    BannerUrl = request.BannerUrl,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Status = request.Status,
                    ViewCount = 0,
                    IsFeatured = request.IsFeatured,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = organizerId,
                    PublishedAt = request.PublishedAt
                };

                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new EventResponse
                {
                    Id = newEvent.Id,
                    OrganizerId = newEvent.OrganizerId,
                    CategoryId = newEvent.CategoryId,
                    VenueId = newEvent.VenueId,
                    Title = newEvent.Title,
                    Slug = newEvent.Slug,
                    Description = newEvent.Description,
                    Summary = newEvent.Summary,
                    ImageUrl = newEvent.ImageUrl,
                    BannerUrl = newEvent.BannerUrl,
                    StartTime = newEvent.StartTime,
                    EndTime = newEvent.EndTime,
                    Status = newEvent.Status,
                    ViewCount = newEvent.ViewCount,
                    IsFeatured = newEvent.IsFeatured,
                    CreatedAt = newEvent.CreatedAt,
                    CreatedBy = newEvent.CreatedBy,
                    PublishedAt = newEvent.PublishedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task<bool> DeleteEventAsync(Guid eventId, Guid organizerId)
        {
            throw new NotImplementedException();
        }

        public async Task<EventResponse> GetEventByIdAsync(Guid eventId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var eventResponse = new EventResponse
            {
                Id = eventEntity.Id,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                VenueId = eventEntity.VenueId,
                Title = eventEntity.Title,
                Slug = eventEntity.Slug,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                ImageUrl = eventEntity.ImageUrl,
                BannerUrl = eventEntity.BannerUrl,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Status = eventEntity.Status,
                ViewCount = eventEntity.ViewCount,
                IsFeatured = eventEntity.IsFeatured,
                CreatedAt = eventEntity.CreatedAt,
                CreatedBy = eventEntity.CreatedBy,
                PublishedAt = eventEntity.PublishedAt
            };

            return eventResponse;
        }

        public async Task<PaginationResponse<EventResponse>> GetEventsAsync(FIlterEventRequest request)
        {
            var events = _context.Events.AsQueryable();
            if (events == null) throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            if (request.CategoryId.HasValue)
                events = events.Where(e => e.CategoryId == request.CategoryId.Value);
            if (request.VenueId.HasValue)
                events = events.Where(e => e.VenueId == request.VenueId.Value);
            if (!string.IsNullOrEmpty(request.Search))
                events = events.Where(e => e.Title.Contains(request.Search));
            if (request.MinPrice != null || request.MaxPrice != null)
            {
                if (request.MinPrice < 0 || request.MaxPrice <= 0 || request.MinPrice > request.MaxPrice)
                {
                    throw new BadRequestException(SystemError.INVALID_PRICE_RANGE);
                }
                events = events.Where(e => e.TicketTypes.Any(t => (request.MinPrice == null || t.Price >= request.MinPrice) && (request.MaxPrice == null || t.Price <= request.MaxPrice)));
            }

            if (request.FromDate != null || request.ToDate != null)
            {
                if (request.FromDate > request.ToDate)
                {
                    throw new BadRequestException(SystemError.INVALID_DATE_RANGE);
                }

                var startOfDay = request.FromDate?.Date;
                var endOfDay = request.ToDate?.Date.AddDays(1).AddTicks(-1);

                events = events.Where(e => (startOfDay == null || e.StartTime >= startOfDay) && (e.EndTime <= endOfDay || endOfDay == null));
            }
            if (!string.IsNullOrEmpty(request.Status))
                events = events.Where(e => e.Status == request.Status);
            if (request.IsFeatured.HasValue)
                events = events.Where(e => e.IsFeatured == request.IsFeatured.Value);

            var response = events.Select(e => new EventResponse
            {
                Id = e.Id,
                OrganizerId = e.OrganizerId,
                CategoryId = e.CategoryId,
                VenueId = e.VenueId,
                Title = e.Title,
                Slug = e.Slug,
                Description = e.Description,
                Summary = e.Summary,
                ImageUrl = e.ImageUrl,
                BannerUrl = e.BannerUrl,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Status = e.Status,
                ViewCount = e.ViewCount,
                IsFeatured = e.IsFeatured,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy,
                PublishedAt = e.PublishedAt
            });

            var responseList = await response.GetPaged(request.CurrentPage, request.PageSize);


            return responseList;
        }

        public async Task<PaginationResponse<EventResponse>> GetEventsByOrganizerAsync(Guid organizerId, PaginationRequest<EventResponse> request)
        {
            var events = _context.Events.Where(e => e.OrganizerId == organizerId).AsQueryable();
            if (!events.Any()) throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var eventResponse = events.Select(e => new EventResponse
            {
                Id = e.Id,
                OrganizerId = e.OrganizerId,
                CategoryId = e.CategoryId,
                VenueId = e.VenueId,
                Title = e.Title,
                Slug = e.Slug,
                Description = e.Description,
                Summary = e.Summary,
                ImageUrl = e.ImageUrl,
                BannerUrl = e.BannerUrl,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Status = e.Status,
                ViewCount = e.ViewCount,
                IsFeatured = e.IsFeatured,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy,
                PublishedAt = e.PublishedAt
            });

            var response = await eventResponse.GetPaged(request.CurrentPage, request.PageSize);
            return response;
        }

        public async Task<EventResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid organizerId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.CategoryId = request.CategoryId;
            eventEntity.VenueId = request.VenueId;
            eventEntity.Title = request.Title;
            eventEntity.Slug = request.Slug;
            eventEntity.Description = request.Description;
            eventEntity.Summary = request.Summary;
            eventEntity.StartTime = request.StartTime;
            eventEntity.EndTime = request.EndTime;
            eventEntity.Status = request.Status;
            eventEntity.IsFeatured = request.IsFeatured;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            eventEntity.UpdatedBy = organizerId;

            _context.Events.Update(eventEntity);
            _context.SaveChanges();

            return new EventResponse
            {
                Id = eventEntity.Id,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                VenueId = eventEntity.VenueId,
                Title = eventEntity.Title,
                Slug = eventEntity.Slug,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                ImageUrl = eventEntity.ImageUrl,
                BannerUrl = eventEntity.BannerUrl,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Status = eventEntity.Status,
                ViewCount = eventEntity.ViewCount,
                IsFeatured = eventEntity.IsFeatured,
                CreatedAt = eventEntity.CreatedAt,
                CreatedBy = eventEntity.CreatedBy,
                UpdatedAt = eventEntity.UpdatedAt,
                UpdatedBy = eventEntity.UpdatedBy,
                PublishedAt = eventEntity.PublishedAt
            };
        }

        public async Task<EventResponse> UpLoadBannerAsync(Guid eventId, string bannerUrl, Guid organizerId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.BannerUrl = bannerUrl;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            eventEntity.UpdatedBy = organizerId;
            _context.Events.Update(eventEntity);
            _context.SaveChanges();

            return new EventResponse
            {
                Id = eventEntity.Id,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                VenueId = eventEntity.VenueId,
                Title = eventEntity.Title,
                Slug = eventEntity.Slug,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                ImageUrl = eventEntity.ImageUrl,
                BannerUrl = eventEntity.BannerUrl,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Status = eventEntity.Status,
                ViewCount = eventEntity.ViewCount,
                IsFeatured = eventEntity.IsFeatured,
                CreatedAt = eventEntity.CreatedAt,
                CreatedBy = eventEntity.CreatedBy,
                UpdatedAt = eventEntity.UpdatedAt,
                UpdatedBy = eventEntity.UpdatedBy,
                PublishedAt = eventEntity.PublishedAt
            };
        }

        public async Task<EventResponse> UpLoadImageAsync(Guid eventId, string imageUrl, Guid organizerId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.ImageUrl = imageUrl;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            eventEntity.UpdatedBy = organizerId;
            _context.Events.Update(eventEntity);
            _context.SaveChanges();

            return new EventResponse
            {
                Id = eventEntity.Id,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                VenueId = eventEntity.VenueId,
                Title = eventEntity.Title,
                Slug = eventEntity.Slug,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                ImageUrl = eventEntity.ImageUrl,
                BannerUrl = eventEntity.BannerUrl,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Status = eventEntity.Status,
                ViewCount = eventEntity.ViewCount,
                IsFeatured = eventEntity.IsFeatured,
                CreatedAt = eventEntity.CreatedAt,
                CreatedBy = eventEntity.CreatedBy,
                UpdatedAt = eventEntity.UpdatedAt,
                UpdatedBy = eventEntity.UpdatedBy,
                PublishedAt = eventEntity.PublishedAt
            };
        }
    }


}

