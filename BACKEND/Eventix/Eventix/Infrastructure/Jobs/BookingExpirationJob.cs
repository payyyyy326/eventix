using System.Data;
using System.Globalization;
using System.Net;
using Eventix.Data;
using Eventix.Infrastructure.Email;
using Eventix.Share.Booking;
using Eventix.Share.Common.Constants;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Eventix.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class BookingExpirationJob : IJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingExpirationJob> _logger;

    public BookingExpirationJob(
        AppDbContext context,
        IEmailService emailService,
        ILogger<BookingExpirationJob> logger)
    {
        _context = context;
        _emailService = emailService;
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

            await TrySendExpiredBookingEmailsAsync(
                bookings,
                context.CancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    private async Task TrySendExpiredBookingEmailsAsync(
        IReadOnlyCollection<Eventix.Entities.Reservation> bookings,
        CancellationToken cancellationToken)
    {
        var groups = bookings.GroupBy(booking =>
            booking.OrderId?.ToString() ?? booking.Id.ToString());

        foreach (var group in groups)
        {
            try
            {
                var userId = group.First().UserId;
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Id == userId)
                    .Select(x => new { x.Email, x.FullName })
                    .FirstOrDefaultAsync(cancellationToken);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    continue;

                var reservationIds = group.Select(x => x.Id).ToList();
                var details = await (
                    from reservation in _context.Reservations
                    join eventEntity in _context.Events
                        on reservation.EventId equals eventEntity.Id
                    join ticketType in _context.TicketTypes
                        on reservation.TicketTypeId equals ticketType.Id
                    join seat in _context.Seats
                        on reservation.SeatId equals seat.Id into seats
                    from seat in seats.DefaultIfEmpty()
                    where reservationIds.Contains(reservation.Id)
                    select new BookingResponse
                    {
                        EventTitle = eventEntity.Title,
                        TicketTypeName = ticketType.Name,
                        SeatLabel = seat == null
                            ? null
                            : ((seat.Section ?? "") + " " +
                               (seat.Row ?? "") + "-" + seat.Number).Trim(),
                        Quantity = reservation.Quantity,
                        UnitPrice = ticketType.Price,
                        TotalAmount = ticketType.Price * reservation.Quantity,
                        ExpiresAt = reservation.ExpiresAt
                    })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                if (details.Count == 0)
                    continue;

                var rows = string.Join("", details.Select(item =>
                {
                    var seat = string.IsNullOrWhiteSpace(item.SeatLabel)
                        ? "Không có số ghế"
                        : WebUtility.HtmlEncode(item.SeatLabel);
                    var amount = item.TotalAmount.ToString(
                        "N0",
                        CultureInfo.GetCultureInfo("vi-VN"));
                    return $"""
                        <tr>
                            <td style="padding:8px;border-bottom:1px solid #eee">{WebUtility.HtmlEncode(item.EventTitle)}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee">{WebUtility.HtmlEncode(item.TicketTypeName)}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee">{seat}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee;text-align:right">{amount} VND</td>
                        </tr>
                        """;
                }));
                var total = details.Sum(x => x.TotalAmount).ToString(
                    "N0",
                    CultureInfo.GetCultureInfo("vi-VN"));
                var eventNames = string.Join(
                    ", ",
                    details.Select(x => x.EventTitle).Distinct());
                var expiredAt = details.Max(x => x.ExpiresAt)
                    .AddHours(7)
                    .ToString("dd/MM/yyyy HH:mm");
                var subject = $"[Eventix] Đặt vé thất bại - {eventNames}";
                var body = $"""
                    <div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;color:#222">
                        <div style="background:#dc3545;color:#fff;padding:22px;border-radius:10px 10px 0 0">
                            <h2 style="margin:0">Lượt giữ vé đã hết hạn</h2>
                        </div>
                        <div style="padding:24px;border:1px solid #ddd;border-top:0;border-radius:0 0 10px 10px">
                            <p>Xin chào <strong>{WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
                            <p>Bạn chưa thanh toán trước <strong>{expiredAt} (GMT+7)</strong>, vì vậy lượt đặt vé không thành công. Vé và ghế đã được trả lại hệ thống.</p>
                            <table style="width:100%;border-collapse:collapse">
                                <thead><tr style="background:#fff0f0">
                                    <th style="padding:8px;text-align:left">Sự kiện</th>
                                    <th style="padding:8px;text-align:left">Hạng vé</th>
                                    <th style="padding:8px;text-align:left">Ghế</th>
                                    <th style="padding:8px;text-align:right">Giá trị</th>
                                </tr></thead>
                                <tbody>{rows}</tbody>
                            </table>
                            <p style="text-align:right">Tổng giá trị: <strong>{total} VND</strong></p>
                            <p>Bạn có thể quay lại Eventix để đặt vé mới nếu vé vẫn còn.</p>
                        </div>
                    </div>
                    """;
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Expired booking email failed for group {BookingGroup}",
                    group.Key);
            }
        }
    }
}
