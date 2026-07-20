using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.TicketTypeModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.TicketType;
using Microsoft.EntityFrameworkCore;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Modules.TicketTypeModule.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly AppDbContext _context;
        public TicketTypeService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<TicketTypeResponse> CreateTicketTypeAsync(Guid eventId, CreateTicketTypeRequest request, Guid userId)
        {
            var eventExist = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventExist == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var ticketTypeExist = await _context.TicketTypes
                .AnyAsync(tt => tt.Name == request.Name && tt.EventId == eventId);

            if (ticketTypeExist)
                throw new BadRequestException(SystemError.TICKET_TYPE_EXIST);

            if (request.Price < 0)
                throw new BadRequestException(SystemError.INVALID_PRICE_RANGE);

            if (request.Quantity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            if (request.SaleStartTime >= request.SaleEndTime)
                throw new BadRequestException(SystemError.INVALID_SALE_TIME);

            if (request.SaleEndTime > eventExist.StartTime)
                throw new BadRequestException(SystemError.SALE_END_TIME_INVALID);

            if (request.VenueZoneId == null || request.VenueZoneId == Guid.Empty)
                throw new BadRequestException(SystemError.SECTION_REQUIRED);

            var zone = await _context.VenueZones
                .FirstOrDefaultAsync(z =>
                    z.Id == request.VenueZoneId &&
                    z.VenueId == eventExist.VenueId);

            if (zone == null)
                throw new BadRequestException(SystemError.SECTION_NOT_FOUND);

            if (request.IsSeatRequired && !zone.HasSeats)
                throw new BadRequestException(SystemError.SECTION_NOT_FOUND);

            if (!request.IsSeatRequired && zone.HasSeats)
                throw new BadRequestException(SystemError.INVALID_SECTION);

            if (request.Quantity > zone.Capacity)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var currentZoneTicketQuantity = await _context.TicketTypes
                .Where(tt =>
                    tt.EventId == eventId &&
                    tt.VenueZoneId == zone.Id)
                .SumAsync(tt => tt.Quantity);

            if (currentZoneTicketQuantity + request.Quantity > zone.Capacity)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var ticketType = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = request.Name.Trim(),
                Description = request.Description,
                Price = request.Price,
                Quantity = request.Quantity,
                Status = TicketTypeStatus.Active,
                SoldQuantity = 0,
                ReservedQuantity = 0,

                VenueZoneId = zone.Id,
                Section = zone.Name,

                SaleStartTime = request.SaleStartTime,
                SaleEndTime = request.SaleEndTime,
                IsSeatRequired = request.IsSeatRequired,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.TicketTypes.Add(ticketType);
            await _context.SaveChangesAsync();

            return new TicketTypeResponse
            {
                Id = ticketType.Id,
                EventId = ticketType.EventId,
                Name = ticketType.Name,
                Description = ticketType.Description,
                Price = ticketType.Price,
                Quantity = ticketType.Quantity,
                SoldQuantity = ticketType.SoldQuantity,
                ReservedQuantity = ticketType.ReservedQuantity,

                VenueZoneId = zone.Id,
                ZoneName = zone.Name,
                HasSeats = zone.HasSeats,
                Section = zone.Name,

                Status = ticketType.Status,
                SaleStartTime = ticketType.SaleStartTime,
                SaleEndTime = ticketType.SaleEndTime,
                IsSeatRequired = ticketType.IsSeatRequired,
                CreatedAt = ticketType.CreatedAt,
                CreatedBy = ticketType.CreatedBy
            };
        }

        public Task DeleteTicketTypeAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<TicketTypeResponse> GetTicketTypeByIdAsync(Guid id)
        {
            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt => tt.Id == id);
            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            var response = new TicketTypeResponse
            {
                Id = ticketType.Id,
                EventId = ticketType.EventId,
                Name = ticketType.Name,
                Description = ticketType.Description,
                Price = ticketType.Price,
                Quantity = ticketType.Quantity,
                SoldQuantity = ticketType.SoldQuantity,
                ReservedQuantity = ticketType.ReservedQuantity,
                Section = ticketType.Section,
                SaleStartTime = ticketType.SaleStartTime,
                SaleEndTime = ticketType.SaleEndTime,
                IsSeatRequired = ticketType.IsSeatRequired
            };

            return response;
        }

        public async Task<TicketTypeResponse> GetTicketTypeByIdForOrganizerAsync(Guid userId, Guid ticketTypeId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt =>
                    tt.Id == ticketTypeId &&
                    tt.Event.OrganizerId == organizer.Id);

            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            return new TicketTypeResponse
            {
                Id = ticketType.Id,
                EventId = ticketType.EventId,

                Name = ticketType.Name,
                Description = ticketType.Description,

                Price = ticketType.Price,

                Quantity = ticketType.Quantity,
                SoldQuantity = ticketType.SoldQuantity,
                ReservedQuantity = ticketType.ReservedQuantity,

                Section = ticketType.Section,

                SaleStartTime = ticketType.SaleStartTime,
                SaleEndTime = ticketType.SaleEndTime,

                IsSeatRequired = ticketType.IsSeatRequired,

                CreatedAt = ticketType.CreatedAt,
                CreatedBy = ticketType.CreatedBy,

                UpdatedAt = ticketType.UpdatedAt,
                UpdatedBy = ticketType.UpdatedBy
            };
        }

        public async Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByEventIdAsync(Guid eventId, PaginationRequest<TicketTypeResponse> request)
        {
            var eventExist = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventExist == null) throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var ticketTypes = _context.TicketTypes
                .Where(tt => tt.EventId == eventId)
                .AsQueryable();

            var ticketTypeResponse = ticketTypes.Select(tt => new TicketTypeResponse
            {
                Id = tt.Id,
                EventId = tt.EventId,
                Name = tt.Name,
                Description = tt.Description,
                Price = tt.Price,
                Quantity = tt.Quantity,
                SoldQuantity = tt.SoldQuantity,
                ReservedQuantity = tt.ReservedQuantity,
                Section = tt.Section,
                SaleStartTime = tt.SaleStartTime,
                SaleEndTime = tt.SaleEndTime,
                IsSeatRequired = tt.IsSeatRequired
            });

            var response = await ticketTypeResponse.GetPaged(request.CurrentPage, request.PageSize);

            return response;
        }

        public async Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByOrganizerEventAsync(Guid userId, Guid eventId, PaginationRequest<TicketTypeResponse> request)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var eventExist = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.Id == eventId &&
                    e.OrganizerId == organizer.Id);

            if (eventExist == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var ticketTypeResponse = _context.TicketTypes
                .Where(tt => tt.EventId == eventId)
                .OrderBy(tt => tt.Price)
                .Select(tt => new TicketTypeResponse
                {
                    Id = tt.Id,
                    EventId = tt.EventId,
                    Name = tt.Name,
                    Description = tt.Description,
                    Price = tt.Price,
                    Quantity = tt.Quantity,
                    SoldQuantity = tt.SoldQuantity,
                    ReservedQuantity = tt.ReservedQuantity,
                    VenueZoneId = tt.VenueZoneId,
                    ZoneName = tt.VenueZone != null ? tt.VenueZone.Name : tt.Section,
                    HasSeats = tt.VenueZone != null && tt.VenueZone.HasSeats,
                    Section = tt.Section,
                    Status = tt.Status,
                    SaleStartTime = tt.SaleStartTime,
                    SaleEndTime = tt.SaleEndTime,
                    IsSeatRequired = tt.IsSeatRequired,
                    CreatedAt = tt.CreatedAt,
                    CreatedBy = tt.CreatedBy,
                    UpdatedAt = tt.UpdatedAt,
                    UpdatedBy = tt.UpdatedBy
                });

            return await ticketTypeResponse.GetPaged(
                request.CurrentPage,
                request.PageSize);
        }

        public async Task<TicketTypeResponse> UpdateTicketTypeAsync(
    Guid id,
    UpdateTicketTypeRequest request,
    Guid userId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .Include(tt => tt.VenueZone)
                .FirstOrDefaultAsync(tt =>
                    tt.Id == id &&
                    tt.Event.OrganizerId == organizer.Id);

            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (request.Price < 0)
                throw new BadRequestException(SystemError.INVALID_PRICE_RANGE);

            if (request.Quantity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var usedQuantity = ticketType.SoldQuantity + ticketType.ReservedQuantity;

            if (request.Quantity < usedQuantity)
                throw new BadRequestException(SystemError.QUANTITY_LESS_THAN_USED);

            if (ticketType.VenueZoneId == null || ticketType.VenueZone == null)
                throw new BadRequestException(SystemError.SECTION_NOT_FOUND);

            var zone = ticketType.VenueZone;

            if (request.Quantity > zone.Capacity)
                throw new BadRequestException(SystemError.QUANTITY_EXCEEDS_SECTION_SEATS);

            var otherTicketQuantityInZone = await _context.TicketTypes
                .Where(tt =>
                    tt.EventId == ticketType.EventId &&
                    tt.VenueZoneId == zone.Id &&
                    tt.Id != ticketType.Id)
                .SumAsync(tt => tt.Quantity);

            if (otherTicketQuantityInZone + request.Quantity > zone.Capacity)
                throw new BadRequestException(SystemError.QUANTITY_EXCEEDS_SECTION_SEATS);

            if (request.SaleStartTime >= request.SaleEndTime)
                throw new BadRequestException(SystemError.INVALID_SALE_TIME);

            if (request.SaleEndTime > ticketType.Event.StartTime)
                throw new BadRequestException(SystemError.SALE_END_TIME_INVALID);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                ticketType.Name = request.Name.Trim();
                ticketType.Description = request.Description;
                ticketType.Price = request.Price;
                ticketType.Quantity = request.Quantity;
                ticketType.SaleStartTime = request.SaleStartTime;
                ticketType.SaleEndTime = request.SaleEndTime;
                ticketType.UpdatedAt = DateTime.UtcNow;
                ticketType.UpdatedBy = userId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new TicketTypeResponse
                {
                    Id = ticketType.Id,
                    EventId = ticketType.EventId,
                    Name = ticketType.Name,
                    Description = ticketType.Description,
                    Price = ticketType.Price,
                    Quantity = ticketType.Quantity,
                    SoldQuantity = ticketType.SoldQuantity,
                    ReservedQuantity = ticketType.ReservedQuantity,

                    VenueZoneId = zone.Id,
                    ZoneName = zone.Name,
                    HasSeats = zone.HasSeats,
                    Section = zone.Name,

                    Status = ticketType.Status,
                    SaleStartTime = ticketType.SaleStartTime,
                    SaleEndTime = ticketType.SaleEndTime,
                    IsSeatRequired = ticketType.IsSeatRequired,
                    CreatedAt = ticketType.CreatedAt,
                    CreatedBy = ticketType.CreatedBy,
                    UpdatedAt = ticketType.UpdatedAt,
                    UpdatedBy = ticketType.UpdatedBy
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<TicketTypeResponse> DeactivateTicketTypeAsync(Guid id, Guid userId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt =>
                    tt.Id == id &&
                    tt.Event.OrganizerId == organizer.Id);

            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (ticketType.SoldQuantity > 0 || ticketType.ReservedQuantity > 0)
                throw new BadRequestException(SystemError.TICKET_TYPE_CANNOT_DELETE);

            ticketType.Status = SystemConstants.TicketTypeStatus.Inactive;
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return new TicketTypeResponse
            {
                Id = ticketType.Id,
                EventId = ticketType.EventId,
                Name = ticketType.Name,
                Description = ticketType.Description,
                Price = ticketType.Price,
                Quantity = ticketType.Quantity,
                SoldQuantity = ticketType.SoldQuantity,
                ReservedQuantity = ticketType.ReservedQuantity,
                Section = ticketType.Section,
                SaleStartTime = ticketType.SaleStartTime,
                SaleEndTime = ticketType.SaleEndTime,
                IsSeatRequired = ticketType.IsSeatRequired,
                Status = ticketType.Status
            };
        }
    }
}
