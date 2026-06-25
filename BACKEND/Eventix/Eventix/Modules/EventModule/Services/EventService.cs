using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.EventModule.Interfaces;
using Eventix.Share.Category;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;
using Eventix.Share.TicketType;
using Eventix.Share.Venue;
using Microsoft.EntityFrameworkCore;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Modules.EventModule.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<EventDetailResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(x => x.UserId == organizerId);

            if (organizer == null) throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);
            if (organizer.Status != OrganizerStatus.APPROVED) throw new BadRequestException(SystemError.ORGANIZER_NOT_APPROVED);

            var venue = await _context.Venues.AnyAsync(x => x.Id == request.VenueId);
            if (!venue) throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            var categoryExists = await _context.Categories.AnyAsync(x => x.Id == request.CategoryId);
            if (!categoryExists) throw new BadRequestException(SystemError.CATEGORY_NOT_FOUND);

            var eventExist = await _context.Events.FirstOrDefaultAsync(e => ((e.StartTime > request.StartTime && e.StartTime < request.EndTime) || (e.EndTime > request.StartTime && e.EndTime < request.EndTime) && e.Venue.Id == request.VenueId));
            if (eventExist != null) throw new BadRequestException(SystemError.EVENT_EXIST);

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

                var createdEvent = await _context.Events
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.Organizer)
                    .FirstOrDefaultAsync(e => e.Id == newEvent.Id);

                return new EventDetailResponse
                {
                    Id = createdEvent.Id,
                    Category = new CategoryResponse
                    {
                        Id = createdEvent.CategoryId,
                        Name = createdEvent.Category.Name,
                    },
                    Title = createdEvent.Title,
                    Slug = createdEvent.Slug,
                    Description = createdEvent.Description,
                    Summary = createdEvent.Summary,
                    ImageUrl = createdEvent.ImageUrl,
                    BannerUrl = createdEvent.BannerUrl,
                    StartTime = createdEvent.StartTime,
                    EndTime = createdEvent.EndTime,
                    Status = createdEvent.Status,
                    ViewCount = createdEvent.ViewCount,
                    IsFeatured = createdEvent.IsFeatured,
                    CreatedAt = createdEvent.CreatedAt,
                    CreatedBy = createdEvent.CreatedBy,
                    PublishedAt = createdEvent.PublishedAt,
                    Venue = new VenueResponse
                    {
                        Name = createdEvent.Venue.Name
                    }

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

        public async Task<EventBookingResponse> GetEventBookingAsync(Guid eventId)
        {
            var eventEntity = await _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);
            }

            return new EventBookingResponse
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Slug = eventEntity.Slug,
                BannerUrl = eventEntity.BannerUrl,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,

                Venue = new VenueResponse
                {
                    Id = eventEntity.Venue.Id,
                    Name = eventEntity.Venue.Name,
                    Address = eventEntity.Venue.Address,
                    City = eventEntity.Venue.City
                },

                TicketTypes = eventEntity.TicketTypes
                .Select(t => new TicketTypeResponse
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    Name = t.Name,
                    Description = t.Description,
                    Price = t.Price,

                    Quantity = t.Quantity,
                    SoldQuantity = t.SoldQuantity,
                    ReservedQuantity = t.ReservedQuantity,

                    Section = t.Section,

                    SaleStartTime = t.SaleStartTime,
                    SaleEndTime = t.SaleEndTime,

                    IsSeatRequired = t.IsSeatRequired,

                    CreatedAt = t.CreatedAt,
                    CreatedBy = t.CreatedBy,
                    UpdatedAt = t.UpdatedAt,
                    UpdatedBy = t.UpdatedBy
                })
                .ToList()
            };
        }

        public async Task<EventDetailResponse> GetEventByIdAsync(Guid eventId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.ViewCount++;
            _context.Events.Update(eventEntity);
            await _context.SaveChangesAsync();

            return new EventDetailResponse
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

                PublishedAt = eventEntity.PublishedAt,

                Category = new CategoryResponse
                {
                    Name = eventEntity.Category.Name,
                },
                Venue = new VenueResponse
                {
                    Name = eventEntity.Venue.Name,
                },
                Organizer = new OrganizerProfileResponse
                {
                    OrganizationName = eventEntity.Organizer.OrganizationName,
                },
                TicketTypes = eventEntity.TicketTypes
                    .OrderBy(t => t.Price)
                    .Select(t => new TicketTypeResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Price = t.Price,
                        Quantity = t.Quantity,
                        Description = t.Description,
                        SaleStartTime = t.SaleStartTime,
                        SaleEndTime = t.SaleEndTime,
                    })
                    .ToList(),
            };
        }

        public async Task<PaginationResponse<EventResponse>> GetEventsAsync(FilterEventRequest request)
        {
            var events = _context.Events.Where(e => e.EndTime > DateTime.UtcNow).AsQueryable();

            if (request.CategoryId.HasValue)
                events = events.Where(e => e.CategoryId == request.CategoryId.Value);

            if (request.VenueId.HasValue)
                events = events.Where(e => e.VenueId == request.VenueId.Value);

            if (!string.IsNullOrEmpty(request.Search))
                events = events.Where(e => e.Title.Trim().ToLower().Contains(request.Search.Trim().ToLower()));

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

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "view":
                        events = events.OrderByDescending(e => e.ViewCount);
                        break;

                    case "upcoming":
                        events = events
                            .Where(e => e.StartTime > DateTime.UtcNow)
                            .OrderBy(e => e.StartTime);
                        break;

                    case "latest":
                        events = events.OrderByDescending(e => e.CreatedAt);
                        break;

                    default:
                        events = events.OrderByDescending(e => e.CreatedAt);
                        break;
                }
            }
            else
            {
                events = events.OrderByDescending(e => e.CreatedAt);
            }

            var response = events.Select(e => new EventResponse
            {
                Id = e.Id,

                Title = e.Title,
                Slug = e.Slug,

                Summary = e.Summary,
                ImageUrl = e.ImageUrl,

                MinPrice = e.TicketTypes
                .Select(t => (decimal?)t.Price)
                .Min(),
                StartTime = e.StartTime,
                EndTime = e.EndTime,

                Status = e.Status,
                ViewCount = e.ViewCount,

                IsFeatured = e.IsFeatured,

                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,

                VenueId = e.VenueId,
                VenueName = e.Venue.Name,
                VenueCity = e.Venue.City,
            });

            var responseList = await response.GetPaged(request.CurrentPage, request.PageSize);


            return responseList;
        }

        public Task<PaginationResponse<EventResponse>> GetEventsByFeatureAsync(FilterEventRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<PaginationResponse<OrganizerEventResponse>> GetEventsByOrganizerAsync(Guid organizerId, PaginationRequest<OrganizerEventResponse> request)
        {
            var events = _context.Events.Where(e => e.OrganizerId == organizerId).AsQueryable();
            if (!events.Any()) throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var eventResponse = events.Select(e => new OrganizerEventResponse
            {
                Id = e.Id,

                Title = e.Title,
                Slug = e.Slug,

                ImageUrl = e.ImageUrl,

                StartTime = e.StartTime,
                EndTime = e.EndTime,

                Status = e.Status,

                ViewCount = e.ViewCount,

                IsFeatured = e.IsFeatured,

                CategoryName = e.Category.Name,
                VenueName = e.Venue.Name,

                CreatedAt = e.CreatedAt,
                PublishedAt = e.PublishedAt
            });

            var response = await eventResponse.GetPaged(request.CurrentPage, request.PageSize);
            return response;
        }

        public async Task<EventDetailResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid organizerId)
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

            return new EventDetailResponse
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

        public async Task<EventDetailResponse> UpLoadBannerAsync(Guid eventId, string bannerUrl, Guid organizerId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.BannerUrl = bannerUrl;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            eventEntity.UpdatedBy = organizerId;
            _context.Events.Update(eventEntity);
            _context.SaveChanges();

            return new EventDetailResponse
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

        public async Task<EventDetailResponse> UpLoadImageAsync(Guid eventId, string imageUrl, Guid organizerId)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            eventEntity.ImageUrl = imageUrl;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            eventEntity.UpdatedBy = organizerId;
            _context.Events.Update(eventEntity);
            _context.SaveChanges();

            return new EventDetailResponse
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

