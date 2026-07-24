using System.Data;
using Eventix.Data;
using Eventix.Share.Common.Constants;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Eventix.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class BookingExpirationJob : IJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingExpirationJob> _logger;

    public BookingExpirationJob(
        AppDbContext context,
        ILogger<BookingExpirationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await _context.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                context.CancellationToken);

        try
        {
            var bookings = await _context.Reservations
                .Where(r =>
                    r.Status == SystemConstants.ReservationStatus.ACTIVE &&
                    r.ExpiresAt <= now)
                .ToListAsync(context.CancellationToken);

            if (bookings.Count == 0)
            {
                await transaction.CommitAsync(context.CancellationToken);
                return;
            }

            var ticketTypeIds = bookings
                .Select(r => r.TicketTypeId)
                .Distinct()
                .ToList();

            var ticketTypes = await _context.TicketTypes
                .Where(t => ticketTypeIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, context.CancellationToken);

            foreach (var booking in bookings)
            {
                booking.Status = SystemConstants.ReservationStatus.EXPIRED;

                if (booking.OrderId.HasValue)
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(
                        o => o.Id == booking.OrderId.Value &&
                            o.Status == SystemConstants.OrderStatus.PENDING,
                        context.CancellationToken);
                    if (order != null)
                    {
                        order.Status = SystemConstants.OrderStatus.EXPIRED;
                        order.UpdatedAt = now;
                    }
                }

                if (ticketTypes.TryGetValue(
                    booking.TicketTypeId,
                    out var ticketType))
                {
                    ticketType.ReservedQuantity = Math.Max(
                        0,
                        ticketType.ReservedQuantity - booking.Quantity);
                }

                if (booking.SeatId == null)
                    continue;

                var seatStatus = await _context.EventSeatStatuses
                    .FirstOrDefaultAsync(s =>
                        s.EventId == booking.EventId &&
                        s.SeatId == booking.SeatId.Value,
                        context.CancellationToken);

                if (seatStatus?.Status == SystemConstants.SeatStatus.RESERVED)
                    seatStatus.Status = SystemConstants.SeatStatus.AVAILABLE;
            }

            await _context.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

            _logger.LogInformation(
                "Expired and released {BookingCount} bookings",
                bookings.Count);
        }
        catch
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
}
