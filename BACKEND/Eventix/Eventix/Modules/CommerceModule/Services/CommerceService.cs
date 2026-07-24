using System.Data;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.CommerceModule.Services;

public class CommerceService : ICommerceService
{
    private readonly AppDbContext _context;

    public CommerceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponse> CreateOrderAsync(
        IReadOnlyCollection<Guid> reservationIds,
        Guid userId)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var now = DateTime.UtcNow;
            var uniqueIds = reservationIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            if (uniqueIds.Count == 0)
                throw new BadRequestException("Phải chọn ít nhất một lượt giữ vé.");

            var reservations = await _context.Reservations
                .Where(x => uniqueIds.Contains(x.Id) && x.UserId == userId)
                .ToListAsync();

            if (reservations.Count != uniqueIds.Count)
                throw new NotFoundException("Không tìm thấy lượt giữ vé.");
            if (reservations.Any(x =>
                x.Status != SystemConstants.ReservationStatus.ACTIVE ||
                x.ExpiresAt <= now))
                throw new BadRequestException("Lượt giữ vé đã hết hạn hoặc không còn hiệu lực.");

            var existingOrderIds = reservations
                .Where(x => x.OrderId.HasValue)
                .Select(x => x.OrderId!.Value)
                .Distinct()
                .ToList();
            if (existingOrderIds.Count == 1 &&
                reservations.All(x => x.OrderId == existingOrderIds[0]))
                return await GetOrderInternalAsync(existingOrderIds[0]);
            if (existingOrderIds.Count > 0)
                throw new BadRequestException("Một số ghế đã thuộc đơn hàng khác.");

            var ticketTypeIds = reservations
                .Select(x => x.TicketTypeId)
                .Distinct()
                .ToList();
            var ticketTypes = await _context.TicketTypes
                .Where(x => ticketTypeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);
            var subTotal = reservations.Sum(x =>
                ticketTypes[x.TicketTypeId].Price * x.Quantity);
            var serviceFee = Math.Round(subTotal * 0.02m, 0);
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderCode = $"EVX-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
                Status = SystemConstants.OrderStatus.PENDING,
                SubTotal = subTotal,
                ServiceFee = serviceFee,
                DiscountAmount = 0,
                TotalAmount = subTotal + serviceFee,
                ExpiresAt = reservations.Min(x => x.ExpiresAt),
                CreatedAt = now
            };
            foreach (var reservation in reservations)
            {
                var unitPrice = ticketTypes[reservation.TicketTypeId].Price;
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    EventId = reservation.EventId,
                    TicketTypeId = reservation.TicketTypeId,
                    SeatId = reservation.SeatId,
                    Quantity = reservation.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice * reservation.Quantity
                });
                reservation.OrderId = order.Id;
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return await GetOrderInternalAsync(order.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderResponse>> GetMyOrdersAsync(Guid userId)
    {
        var ids = await _context.Orders
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync();
        var result = new List<OrderResponse>();
        foreach (var id in ids)
            result.Add(await GetOrderInternalAsync(id));
        return result;
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId)
    {
        if (!await _context.Orders.AnyAsync(x => x.Id == orderId && x.UserId == userId))
            throw new NotFoundException("Không tìm thấy đơn hàng.");
        return await GetOrderInternalAsync(orderId);
    }

    public async Task<PaymentResponse> CompleteDemoPaymentAsync(Guid orderId, Guid userId)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var now = DateTime.UtcNow;
            var order = await _context.Orders
                .Include(x => x.OrderItems)
                .Include(x => x.Reservations)
                .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);

            if (order == null)
                throw new NotFoundException("Không tìm thấy đơn hàng.");
            if (order.Status == SystemConstants.OrderStatus.PAID)
            {
                var completed = await _context.Payments
                    .AsNoTracking()
                    .FirstAsync(x => x.OrderId == order.Id &&
                        x.Status == SystemConstants.PaymentStatus.SUCCESS);
                return MapPayment(completed);
            }
            if (order.Status != SystemConstants.OrderStatus.PENDING ||
                order.ExpiresAt <= now)
                throw new BadRequestException("Đơn hàng đã hết hạn hoặc không thể thanh toán.");

            foreach (var reservation in order.Reservations)
            {
                if (reservation.Status != SystemConstants.ReservationStatus.ACTIVE)
                    throw new BadRequestException("Lượt giữ vé không còn hiệu lực.");

                var ticketType = await _context.TicketTypes
                    .FirstAsync(x => x.Id == reservation.TicketTypeId);
                ticketType.ReservedQuantity = Math.Max(
                    0, ticketType.ReservedQuantity - reservation.Quantity);
                ticketType.SoldQuantity += reservation.Quantity;
                reservation.Status = SystemConstants.ReservationStatus.CONFIRMED;

                if (reservation.SeatId.HasValue)
                {
                    var seatStatus = await _context.EventSeatStatuses.FirstAsync(x =>
                        x.EventId == reservation.EventId &&
                        x.SeatId == reservation.SeatId.Value);
                    seatStatus.Status = SystemConstants.SeatStatus.SOLD;
                }

                for (var index = 0; index < reservation.Quantity; index++)
                {
                    var token = Guid.NewGuid().ToString("N");
                    await _context.Tickets.AddAsync(new Ticket
                    {
                        Id = Guid.NewGuid(),
                        EventId = reservation.EventId,
                        TicketTypeId = reservation.TicketTypeId,
                        OrderId = order.Id,
                        UserId = userId,
                        SeatId = reservation.SeatId,
                        TicketCode = $"EVT-{token[..10].ToUpperInvariant()}",
                        QrToken = token,
                        Status = SystemConstants.TicketStatus.ACTIVE,
                        IssuedAt = now
                    });
                }
            }

            order.Status = SystemConstants.OrderStatus.PAID;
            order.PaidAt = now;
            order.UpdatedAt = now;
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = userId,
                Gateway = "Demo",
                TransactionCode = $"DEMO-{Guid.NewGuid():N}".ToUpperInvariant(),
                GatewayTransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = "VND",
                Status = SystemConstants.PaymentStatus.SUCCESS,
                PaidAt = now,
                CreatedAt = now
            };
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapPayment(payment);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TicketResponse>> GetMyTicketsAsync(Guid userId)
    {
        return await TicketQuery()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => x.Response)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TicketResponse> GetTicketAsync(Guid ticketId, Guid userId)
    {
        var result = await TicketQuery()
            .Where(x => x.Id == ticketId && x.UserId == userId)
            .Select(x => x.Response)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return result ?? throw new NotFoundException("Không tìm thấy vé.");
    }

    public async Task<List<TicketResponse>> GetEventTicketsAsync(
        Guid eventId, Guid staffUserId, bool isAdmin)
    {
        await EnsureEventPermissionAsync(eventId, staffUserId, isAdmin);
        return await TicketQuery()
            .Where(x => x.Response.EventId == eventId)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => x.Response)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CheckInResponse> CheckInAsync(
        CheckInRequest request, Guid staffUserId, bool isAdmin)
    {
        await EnsureEventPermissionAsync(request.EventId, staffUserId, isAdmin);
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(x => x.QrToken == request.QrToken);
            if (ticket == null || ticket.EventId != request.EventId)
                throw new NotFoundException("Mã QR không hợp lệ cho sự kiện này.");
            if (ticket.Status != SystemConstants.TicketStatus.ACTIVE)
                throw new BadRequestException("Vé đã được sử dụng hoặc đã bị hủy.");

            var now = DateTime.UtcNow;
            ticket.Status = SystemConstants.TicketStatus.USED;
            ticket.CheckedInAt = now;
            await _context.CheckInLogs.AddAsync(new CheckInLog
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                EventId = ticket.EventId,
                CheckedInBy = staffUserId,
                CheckInTime = now,
                Method = "QR"
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var detail = await (
                from t in _context.Tickets
                join u in _context.Users on t.UserId equals u.Id
                join tt in _context.TicketTypes on t.TicketTypeId equals tt.Id
                join s in _context.Seats on t.SeatId equals s.Id into seats
                from s in seats.DefaultIfEmpty()
                where t.Id == ticket.Id
                select new CheckInResponse
                {
                    TicketId = t.Id,
                    TicketCode = t.TicketCode,
                    CustomerName = u.FullName,
                    TicketTypeName = tt.Name,
                    SeatLabel = s == null ? null :
                        ((s.Section ?? "") + " " + (s.Row ?? "") + "-" + s.Number).Trim(),
                    CheckedInAt = now
                }).AsNoTracking().FirstAsync();
            return detail;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CheckInStatsResponse> GetCheckInStatsAsync(
        Guid eventId, Guid staffUserId, bool isAdmin)
    {
        await EnsureEventPermissionAsync(eventId, staffUserId, isAdmin);
        var total = await _context.Tickets.CountAsync(x =>
            x.EventId == eventId &&
            x.Status != SystemConstants.TicketStatus.CANCELLED);
        var checkedIn = await _context.Tickets.CountAsync(x =>
            x.EventId == eventId &&
            x.Status == SystemConstants.TicketStatus.USED);
        return new CheckInStatsResponse
        {
            EventId = eventId,
            TotalTickets = total,
            CheckedInTickets = checkedIn,
            RemainingTickets = total - checkedIn
        };
    }

    private async Task EnsureEventPermissionAsync(Guid eventId, Guid userId, bool isAdmin)
    {
        if (isAdmin)
            return;
        var allowed = await _context.Events.AnyAsync(x =>
            x.Id == eventId && x.Organizer.UserId == userId);
        if (!allowed)
            throw new ForbiddenException("Bạn không quản lý sự kiện này.");
    }

    private async Task<OrderResponse> GetOrderInternalAsync(Guid orderId)
    {
        var order = await _context.Orders.AsNoTracking()
            .FirstAsync(x => x.Id == orderId);
        var items = await (
            from item in _context.OrderItems
            join e in _context.Events on item.EventId equals e.Id
            join tt in _context.TicketTypes on item.TicketTypeId equals tt.Id
            join s in _context.Seats on item.SeatId equals s.Id into seats
            from s in seats.DefaultIfEmpty()
            where item.OrderId == orderId
            select new OrderItemResponse
            {
                EventId = item.EventId,
                EventTitle = e.Title,
                TicketTypeName = tt.Name,
                SeatLabel = s == null ? null :
                    ((s.Section ?? "") + " " + (s.Row ?? "") + "-" + s.Number).Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).AsNoTracking().ToListAsync();
        return new OrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            Status = order.Status,
            SubTotal = order.SubTotal,
            ServiceFee = order.ServiceFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            ExpiresAt = order.ExpiresAt,
            PaidAt = order.PaidAt,
            CreatedAt = order.CreatedAt,
            Items = items
        };
    }

    private IQueryable<TicketProjection> TicketQuery()
    {
        return
            from ticket in _context.Tickets
            join eventEntity in _context.Events on ticket.EventId equals eventEntity.Id
            join venue in _context.Venues on eventEntity.VenueId equals venue.Id
            join ticketType in _context.TicketTypes on ticket.TicketTypeId equals ticketType.Id
            join seat in _context.Seats on ticket.SeatId equals seat.Id into seats
            from seat in seats.DefaultIfEmpty()
            select new TicketProjection
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                IssuedAt = ticket.IssuedAt,
                Response = new TicketResponse
                {
                    Id = ticket.Id,
                    EventId = ticket.EventId,
                    EventTitle = eventEntity.Title,
                    EventStartTime = eventEntity.StartTime,
                    VenueName = venue.Name,
                    TicketTypeName = ticketType.Name,
                    SeatLabel = seat == null ? null :
                        ((seat.Section ?? "") + " " + (seat.Row ?? "") + "-" + seat.Number).Trim(),
                    TicketCode = ticket.TicketCode,
                    QrToken = ticket.QrToken,
                    Status = ticket.Status,
                    IssuedAt = ticket.IssuedAt,
                    CheckedInAt = ticket.CheckedInAt
                }
            };
    }

    private static PaymentResponse MapPayment(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Gateway = payment.Gateway,
        TransactionCode = payment.TransactionCode,
        Amount = payment.Amount,
        Status = payment.Status,
        PaidAt = payment.PaidAt
    };

    private sealed class TicketProjection
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime IssuedAt { get; set; }
        public TicketResponse Response { get; set; } = null!;
    }
}
