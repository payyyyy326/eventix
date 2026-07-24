using System.Data;
using System.Globalization;
using System.Net;
using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Infrastructure.Email;
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
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<BookingService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
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

            await TrySendBookingEmailAsync(
                userId,
                responses,
                isCancelled: false);

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

            var cancelledBookings = new List<BookingResponse>();
            foreach (var item in bookingsToCancel)
                cancelledBookings.Add(await GetBookingResponseAsync(item));

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

            await TrySendBookingEmailAsync(
                userId,
                cancelledBookings,
                isCancelled: true);
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

    private async Task TrySendBookingEmailAsync(
        Guid userId,
        IReadOnlyCollection<BookingResponse> bookings,
        bool isCancelled)
    {
        if (bookings.Count == 0)
            return;

        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.FullName })
                .FirstOrDefaultAsync();

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning(
                    "Cannot send booking email because user {UserId} has no email",
                    userId);
                return;
            }

            var firstBooking = bookings.First();
            var eventTitle = WebUtility.HtmlEncode(firstBooking.EventTitle);
            var customerName = WebUtility.HtmlEncode(user.FullName);
            var ticketRows = string.Join(
                "",
                bookings.Select(item =>
                {
                    var ticketName = WebUtility.HtmlEncode(item.TicketTypeName);
                    var seat = string.IsNullOrWhiteSpace(item.SeatLabel)
                        ? "Không có số ghế"
                        : WebUtility.HtmlEncode(item.SeatLabel);
                    var amount = item.TotalAmount.ToString(
                        "N0",
                        CultureInfo.GetCultureInfo("vi-VN"));

                    return $"""
                        <tr>
                            <td style="padding:8px;border-bottom:1px solid #eee">{ticketName}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee">{seat}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee;text-align:center">{item.Quantity}</td>
                            <td style="padding:8px;border-bottom:1px solid #eee;text-align:right">{amount} VND</td>
                        </tr>
                        """;
                }));

            var totalAmount = bookings.Sum(item => item.TotalAmount).ToString(
                "N0",
                CultureInfo.GetCultureInfo("vi-VN"));
            var expiresAt = bookings.Max(item => item.ExpiresAt)
                .AddHours(7)
                .ToString("dd/MM/yyyy HH:mm");

            var subject = isCancelled
                ? $"[Eventix] Đã hủy vé - {firstBooking.EventTitle}"
                : $"[Eventix] Giữ vé thành công - {firstBooking.EventTitle}";
            var title = isCancelled ? "Vé đã được hủy" : "Giữ vé thành công";
            var message = isCancelled
                ? "Yêu cầu hủy vé của bạn đã được xử lý. Vé và ghế đã được trả lại hệ thống."
                : $"Vé đang được giữ đến <strong>{expiresAt} (GMT+7)</strong>. Vui lòng thanh toán trước thời hạn này để nhận vé điện tử.";
            var statusColor = isCancelled ? "#dc3545" : "#6f42c1";

            var body = $"""
                <div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;color:#222">
                    <div style="background:{statusColor};color:#fff;padding:22px;border-radius:10px 10px 0 0">
                        <h2 style="margin:0">{title}</h2>
                    </div>
                    <div style="padding:24px;border:1px solid #ddd;border-top:0;border-radius:0 0 10px 10px">
                        <p>Xin chào <strong>{customerName}</strong>,</p>
                        <p>{message}</p>
                        <h3 style="margin-bottom:8px">{eventTitle}</h3>
                        <table style="width:100%;border-collapse:collapse">
                            <thead>
                                <tr style="background:#f5f3ff">
                                    <th style="padding:8px;text-align:left">Hạng vé</th>
                                    <th style="padding:8px;text-align:left">Ghế</th>
                                    <th style="padding:8px">SL</th>
                                    <th style="padding:8px;text-align:right">Thành tiền</th>
                                </tr>
                            </thead>
                            <tbody>{ticketRows}</tbody>
                        </table>
                        <p style="font-size:18px;text-align:right">
                            Tổng cộng: <strong>{totalAmount} VND</strong>
                        </p>
                        <p style="color:#666;font-size:13px">
                            Đây là email tự động từ Eventix, vui lòng không trả lời email này.
                        </p>
                    </div>
                </div>
                """;

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Booking operation succeeded but notification email failed for user {UserId}",
                userId);
        }
     }
}
