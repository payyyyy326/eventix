using Eventix.Data;
using Eventix.Entities;
using Eventix.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Infrastructure.Jobs
{
    public class EventStatusJob : IJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EventStatusJob> _logger;
        private readonly IHubContext<EventHub> _hubContext;

        public EventStatusJob(
            AppDbContext context,
            ILogger<EventStatusJob> logger,
            IHubContext<EventHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var now = DateTime.UtcNow;


            var events = await _context.Events
                .Include(e => e.TicketTypes)
                .Where(e =>
                    e.Status != EventStatus.Cancelled &&
                    e.Status != EventStatus.Completed &&
                    (
                        // Draft đến giờ publish
                        (e.Status == EventStatus.Draft &&
                         e.PublishedAt != null &&
                         e.PublishedAt <= now)

                        ||

                        // Event đến giờ bắt đầu / kết thúc
                        (e.StartTime <= now || e.EndTime <= now)

                        ||

                        // Có ticket type đến giờ mở bán / đóng bán
                        e.TicketTypes.Any(t =>
                            t.SaleStartTime <= now ||
                            t.SaleEndTime <= now)
                    ))
                .ToListAsync(context.CancellationToken);

            foreach (var eventEntity in events)
            {
                var oldStatus = eventEntity.Status;
                var newStatus = CalculateStatus(eventEntity, now);

                if (oldStatus == newStatus)
                    continue;

                eventEntity.Status = newStatus;
                eventEntity.UpdatedAt = now;

                await _hubContext.Clients.All.SendAsync(
                    "EventStatusChanged",
                    new
                    {
                        eventId = eventEntity.Id,
                        title = eventEntity.Title,
                        oldStatus,
                        newStatus
                    },
                    context.CancellationToken);
            }

            await _context.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "EventStatusJob executed at {Time}. Checked {Count} events.",
                now,
                events.Count);
        }

        private static string CalculateStatus(Event eventEntity, DateTime now)
        {
            if (eventEntity.Status == EventStatus.Cancelled)
                return EventStatus.Cancelled;

            if (eventEntity.EndTime <= now)
                return EventStatus.Completed;

            if (eventEntity.StartTime <= now && eventEntity.EndTime > now)
                return EventStatus.Ongoing;

            if (eventEntity.Status == EventStatus.Draft)
            {
                if (eventEntity.PublishedAt.HasValue &&
                    eventEntity.PublishedAt.Value <= now)
                {
                    return EventStatus.Published;
                }

                return EventStatus.Draft;
            }

            var hasTicketType = eventEntity.TicketTypes.Any();

            var isSoldOut =
                hasTicketType &&
                eventEntity.TicketTypes.All(t =>
                    t.Quantity <= t.SoldQuantity + t.ReservedQuantity);

            if (isSoldOut)
                return EventStatus.SoldOut;

            var hasOnSaleTicket = eventEntity.TicketTypes.Any(t =>
                t.SaleStartTime <= now &&
                t.SaleEndTime >= now &&
                t.Quantity > t.SoldQuantity + t.ReservedQuantity);

            if (hasOnSaleTicket)
                return EventStatus.OnSale;

            return EventStatus.Published;
        }
    }
}