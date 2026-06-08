using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Common.Models;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.TicketTypeModule.DTOs;
using Eventix.Modules.TicketTypeModule.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            {
                throw new BadRequestException(
                    SystemError.SALE_END_TIME_INVALID);
            }

            if (request.IsSeatRequired && string.IsNullOrWhiteSpace(request.Section))
                throw new BadRequestException(SystemError.SECTION_REQUIRED);

            var ticketType = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Quantity = request.Quantity,
                SoldQuantity = 0,
                ReservedQuantity = 0,
                Section = request.IsSeatRequired ? request.Section : null,
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
                Section = ticketType.Section,
                SaleStartTime = ticketType.SaleStartTime,
                SaleEndTime = ticketType.SaleEndTime,
                IsSeatRequired = ticketType.IsSeatRequired
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

        public async Task<TicketTypeResponse> UpdateTicketTypeAsync(Guid id, UpdateTicketTypeRequest request, Guid userId)
        {
            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt => tt.Id == id);
            if (ticketType == null) throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (request.Price < 0)
                throw new BadRequestException(SystemError.INVALID_PRICE_RANGE);

            if (request.Quantity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            if (request.SaleStartTime >= request.SaleEndTime)
                throw new BadRequestException(SystemError.INVALID_SALE_TIME);

            if (request.SaleEndTime > ticketType.Event.StartTime)
            {
                throw new BadRequestException(
                    SystemError.SALE_END_TIME_INVALID);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticketType.Name = request.Name;
                ticketType.Description = request.Description;
                ticketType.Price = request.Price;
                ticketType.Quantity = request.Quantity;
                ticketType.SaleStartTime = request.SaleStartTime;
                ticketType.SaleEndTime = request.SaleEndTime;
                ticketType.UpdatedAt = DateTime.UtcNow;
                ticketType.UpdatedBy = userId;

                _context.TicketTypes.Update(ticketType);
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
                    Section = ticketType.Section,
                    SaleStartTime = ticketType.SaleStartTime,
                    SaleEndTime = ticketType.SaleEndTime,
                    IsSeatRequired = ticketType.IsSeatRequired
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
