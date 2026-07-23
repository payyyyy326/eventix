using System.Data;
using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.BookingModule.Interfaces;
using Eventix.Share.Booking;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.BookingModule.Services;

public class BookingService : IBookingService
{
    private static readonly TimeSpan BookingDuration = TimeSpan.FromMinutes(15);
    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BookingResponse>> CreateBookingAsync(
        CreateBookingRequest request,
        Guid userId)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var ticketType = await _context.TicketTypes
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t =>
                    t.Id == request.TicketTypeId &&
                    t.EventId == request.EventId);

            if (ticketType == null)
                throw new NotFoundException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (ticketType.Status != SystemConstants.TicketTypeStatus.Active ||
                now < ticketType.SaleStartTime ||
                now > ticketType.SaleEndTime ||
                ticketType.Event.Status is SystemConstants.EventStatus.Cancelled
                    or SystemConstants.EventStatus.Completed)
            {
                throw new BadRequestException(SystemError.TICKET_NOT_ON_SALE);
            }

            var availableQuantity =
                ticketType.Quantity -
                ticketType.SoldQuantity -
                ticketType.ReservedQuantity;

            if (availableQuantity < request.Quantity)
                throw new BadRequestException(SystemError.TICKET_SOLD_OUT);

            var selectedSeatIds = request.SeatIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            var eventSeatStatuses = new List<EventSeatStatus>();

            if (ticketType.IsSeatRequired)
            {
                if (selectedSeatIds.Count != request.Quantity)
                    throw new BadRequestException(SystemError.SEAT_REQUIRED);

                eventSeatStatuses = await _context.EventSeatStatuses
                    .Where(s =>
                        s.EventId == request.EventId &&
                        s.TicketTypeId == request.TicketTypeId &&
                        selectedSeatIds.Contains(s.SeatId) &&
                        s.Status == SystemConstants.SeatStatus.AVAILABLE)
                    .ToListAsync();

                if (eventSeatStatuses.Count != selectedSeatIds.Count)
                    throw new ConflictException(SystemError.SEAT_NOT_AVAILABLE);
            }
            else if (selectedSeatIds.Count > 0)
            {
                throw new BadRequestException(SystemError.SEAT_NOT_AVAILABLE);
            }

            var expiresAt = now.Add(BookingDuration);
            var bookings = ticketType.IsSeatRequired
                ? eventSeatStatuses.Select(seatStatus => new Reservation
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventId = request.EventId,
                    TicketTypeId = request.TicketTypeId,
                    SeatId = seatStatus.SeatId,
                    Quantity = 1,
                    Status = SystemConstants.ReservationStatus.ACTIVE,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                }).ToList()
                : [
                    new Reservation
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        EventId = request.EventId,
                        TicketTypeId = request.TicketTypeId,
                        Quantity = request.Quantity,
                        Status = SystemConstants.ReservationStatus.ACTIVE,
                        ExpiresAt = expiresAt,
                        CreatedAt = now
                    }
                ];

            ticketType.ReservedQuantity += request.Quantity;
            foreach (var seatStatus in eventSeatStatuses)
                seatStatus.Status = SystemConstants.SeatStatus.RESERVED;

            await _context.Reservations.AddRangeAsync(bookings);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var responses = new List<BookingResponse>();
            foreach (var booking in bookings)
                responses.Add(await GetBookingResponseAsync(booking));
            return responses;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PaginationResponse<BookingResponse>> GetMyBookingsAsync(
        PaginationRequest<BookingResponse> request,
        Guid userId)
    {
        var query =
            from booking in _context.Reservations
            join ticketType in _context.TicketTypes
                on booking.TicketTypeId equals ticketType.Id
            join eventEntity in _context.Events
                on booking.EventId equals eventEntity.Id
            join seat in _context.Seats
                on booking.SeatId equals seat.Id into seats
            from seat in seats.DefaultIfEmpty()
            where booking.UserId == userId
            orderby booking.CreatedAt descending
            select new BookingResponse
            {
                Id = booking.Id,
                EventId = booking.EventId,
                EventTitle = eventEntity.Title,
                TicketTypeId = booking.TicketTypeId,
                TicketTypeName = ticketType.Name,
                SeatId = booking.SeatId,
                SeatLabel = seat == null
                    ? null
                    : ((seat.Section ?? "") + " " +
                       (seat.Row ?? "") + "-" +
                       seat.Number).Trim(),
                Quantity = booking.Quantity,
                UnitPrice = ticketType.Price,
                TotalAmount = ticketType.Price * booking.Quantity,
                Status = booking.Status,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt
            };

        return await query.GetPaged(request.CurrentPage, request.PageSize);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var booking = await _context.Reservations
                .FirstOrDefaultAsync(r =>
                    r.Id == bookingId &&
                    r.UserId == userId);

            if (booking == null)
                throw new NotFoundException(SystemError.BOOKING_NOT_FOUND);

            if (booking.Status != SystemConstants.ReservationStatus.ACTIVE)
                throw new BadRequestException(SystemError.BOOKING_NOT_ACTIVE);

            var bookingsToCancel = booking.OrderId.HasValue
                ? await _context.Reservations
                    .Where(r =>
                        r.OrderId == booking.OrderId &&
                        r.UserId == userId &&
                        r.Status == SystemConstants.ReservationStatus.ACTIVE)
                    .ToListAsync()
                : [booking];

            foreach (var item in bookingsToCancel)
            {
                await ReleaseBookingAsync(
                    item,
                    SystemConstants.ReservationStatus.CANCELLED);
            }

            if (booking.OrderId.HasValue)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o =>
                    o.Id == booking.OrderId.Value &&
                    o.Status == SystemConstants.OrderStatus.PENDING);
                if (order != null)
                {
                    order.Status = SystemConstants.OrderStatus.CANCELLED;
                    order.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ReleaseBookingAsync(Reservation booking, string newStatus)
    {
        var ticketType = await _context.TicketTypes
            .FirstAsync(t => t.Id == booking.TicketTypeId);

        ticketType.ReservedQuantity =
            Math.Max(0, ticketType.ReservedQuantity - booking.Quantity);
        booking.Status = newStatus;

        if (booking.SeatId == null)
            return;

        var seatStatus = await _context.EventSeatStatuses
            .FirstOrDefaultAsync(s =>
                s.EventId == booking.EventId &&
                s.SeatId == booking.SeatId.Value);

        if (seatStatus?.Status == SystemConstants.SeatStatus.RESERVED)
            seatStatus.Status = SystemConstants.SeatStatus.AVAILABLE;
    }

    private async Task<BookingResponse> GetBookingResponseAsync(Reservation booking)
    {
        return await (
            from ticketType in _context.TicketTypes
            join eventEntity in _context.Events
                on ticketType.EventId equals eventEntity.Id
            join seat in _context.Seats
                on booking.SeatId equals seat.Id into seats
            from seat in seats.DefaultIfEmpty()
            where ticketType.Id == booking.TicketTypeId
            select new BookingResponse
            {
                Id = booking.Id,
                EventId = booking.EventId,
                EventTitle = eventEntity.Title,
                TicketTypeId = booking.TicketTypeId,
                TicketTypeName = ticketType.Name,
                SeatId = booking.SeatId,
                SeatLabel = seat == null
                    ? null
                    : ((seat.Section ?? "") + " " +
                       (seat.Row ?? "") + "-" +
                       seat.Number).Trim(),
                Quantity = booking.Quantity,
                UnitPrice = ticketType.Price,
                TotalAmount = ticketType.Price * booking.Quantity,
                Status = booking.Status,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt
            })
            .AsNoTracking()
            .FirstAsync();
    }
}
