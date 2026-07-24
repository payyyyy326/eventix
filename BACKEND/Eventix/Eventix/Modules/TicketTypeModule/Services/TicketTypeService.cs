using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Helpers;
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

        // Bảng màu mặc định cho các ticket type khi không chỉ định màu
        private static readonly string[] DefaultSectionColors =
        {
            "#3B82F6", "#10B981", "#F59E0B", "#EF4444",
            "#8B5CF6", "#EC4899", "#06B6D4", "#84CC16"
        };

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

            // Gán màu mặc định nếu không cung cấp
            var sectionColor = string.IsNullOrWhiteSpace(request.SectionColor)
                ? await PickDefaultColorAsync(eventId)
                : request.SectionColor;

            // Section name = tên ticket type
            var sectionName = request.Name.Trim();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ticketType = new TicketType
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = sectionName,
                    Description = request.Description,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    Status = TicketTypeStatus.Active,
                    SoldQuantity = 0,
                    ReservedQuantity = 0,
                    VenueZoneId = null,
                    Section = sectionName,
                    SaleStartTime = request.SaleStartTime,
                    SaleEndTime = request.SaleEndTime,
                    IsSeatRequired = request.IsSeatRequired,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.TicketTypes.Add(ticketType);
                await _context.SaveChangesAsync();

                // Seats sẽ được generate trong PublishEventAsync, không generate ở đây.

                await UpsertSectionLayoutAsync(
                    eventExist.VenueId,
                    ticketType.Id,
                    sectionName,
                    sectionColor);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToResponse(ticketType, sectionColor);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Auto-generate seats cho ticketType có IsSeatRequired = true.
        /// Ghế được đặt tên: {Row}{Number}, section = tên ticket type.
        /// Sau đó tạo EventSeatStatus tương ứng để buyer có thể chọn ghế.
        /// Skip ghế đã tồn tại để tránh vi phạm UQ_Seats.
        /// </summary>
        private async Task GenerateSeatsForTicketTypeAsync(
            TicketType ticketType,
            Guid venueId,
            int quantity,
            string sectionName)
        {
            // Lấy các ghế đã tồn tại trong section này
            var existingSeats = await _context.Seats
                .Where(s => s.VenueId == venueId && s.Section == sectionName)
                .ToListAsync();

            var existingKeys = existingSeats
                .Select(s => SeatHelper.BuildSeatKey(s.Section, s.Row, s.Number))
                .ToHashSet();

            // Lấy các EventSeatStatus đã tồn tại cho event + ticketType này
            var existingStatusSeatIds = (await _context.EventSeatStatuses
                .Where(e => e.EventId == ticketType.EventId && e.TicketTypeId == ticketType.Id)
                .Select(e => e.SeatId)
                .ToListAsync())
                .ToHashSet();

            // Layout lưới: tính số cột/hàng tối ưu
            var cols = (int)Math.Ceiling(Math.Sqrt(quantity));
            var rows = (int)Math.Ceiling((double)quantity / cols);

            const decimal startX = 20m;
            const decimal startY = 20m;
            const decimal gapX = 40m;
            const decimal gapY = 40m;

            var newSeats = new List<Seat>();
            var seatCount = 0;

            for (int r = 0; r < rows && seatCount < quantity; r++)
            {
                var rowLabel = SeatHelper.IndexToRowLabel(r);

                for (int c = 1; c <= cols && seatCount < quantity; c++)
                {
                    var seatKey = SeatHelper.BuildSeatKey(sectionName, rowLabel, c.ToString());

                    // Skip nếu ghế đã tồn tại (tránh UQ_Seats violation)
                    if (!existingKeys.Contains(seatKey))
                    {
                        newSeats.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            VenueId = venueId,
                            VenueZoneId = null,
                            Section = sectionName,
                            Row = rowLabel,
                            Number = c.ToString(),
                            Xposition = startX + ((c - 1) * gapX),
                            Yposition = startY + (r * gapY),
                            Status = SeatStatus.AVAILABLE
                        });
                    }

                    seatCount++;
                }
            }

            if (newSeats.Any())
            {
                await _context.Seats.AddRangeAsync(newSeats);
                await _context.SaveChangesAsync();
            }

            // Tạo EventSeatStatus cho ghế mới (chưa có status trong event này)
            var allSectionSeats = existingSeats.Concat(newSeats).ToList();

            var newStatuses = allSectionSeats
                .Where(s => !existingStatusSeatIds.Contains(s.Id))
                .Select(seat => new EventSeatStatus
                {
                    Id = Guid.NewGuid(),
                    EventId = ticketType.EventId,
                    SeatId = seat.Id,
                    TicketTypeId = ticketType.Id,
                    Status = SeatStatus.AVAILABLE
                })
                .ToList();

            if (newStatuses.Any())
            {
                await _context.EventSeatStatuses.AddRangeAsync(newStatuses);
            }
        }

        /// <summary>
        /// <summary>
        /// Tạo hoặc cập nhật VenueSectionLayout để hiển thị trên venue map.
        /// Mỗi TicketType là 1 block trên map.
        /// Ưu tiên: tìm theo TicketTypeId → tìm theo Section name (wizard đã save trước) → tạo mới.
        /// </summary>
        private async Task UpsertSectionLayoutAsync(
            Guid venueId,
            Guid ticketTypeId,
            string sectionName,
            string color)
        {
            // 1. Tìm layout đã gán TicketTypeId này
            var existing = await _context.VenueSectionLayouts
                .FirstOrDefaultAsync(l =>
                    l.VenueId == venueId &&
                    l.TicketTypeId == ticketTypeId);

            if (existing != null)
            {
                existing.Section = sectionName;
                existing.Color = color;
                existing.UpdatedAt = DateTime.UtcNow;
                return;
            }

            // 2. Tìm layout có Section trùng tên nhưng chưa gán TicketTypeId
            //    (được lưu từ Step 6 wizard trước khi publish)
            var orphan = await _context.VenueSectionLayouts
                .FirstOrDefaultAsync(l =>
                    l.VenueId == venueId &&
                    l.TicketTypeId == null &&
                    l.Section == sectionName);

            if (orphan != null)
            {
                orphan.TicketTypeId = ticketTypeId;
                orphan.Color = color;
                orphan.UpdatedAt = DateTime.UtcNow;
                return;
            }

            // 3. Không tìm thấy → tạo mới với vị trí auto
            var layoutCount = await _context.VenueSectionLayouts
                .CountAsync(l => l.VenueId == venueId);

            const int blockWidth  = 160;
            const int blockHeight = 120;
            const int padding     = 20;
            const int canvasWidth = 800;

            var col = layoutCount % (canvasWidth / (blockWidth + padding));
            var row = layoutCount / (canvasWidth / (blockWidth + padding));

            _context.VenueSectionLayouts.Add(new VenueSectionLayout
            {
                Id = Guid.NewGuid(),
                VenueId = venueId,
                TicketTypeId = ticketTypeId,
                VenueZoneId = null,
                Section = sectionName,
                X = padding + col * (blockWidth + padding),
                Y = padding + row * (blockHeight + padding),
                Width = blockWidth,
                Height = blockHeight,
                Color = color,
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Chọn màu mặc định cho ticket type mới dựa trên số lượng ticket types đã có.
        /// </summary>
        private async Task<string> PickDefaultColorAsync(Guid eventId)
        {
            var count = await _context.TicketTypes
                .CountAsync(tt => tt.EventId == eventId);

            return DefaultSectionColors[count % DefaultSectionColors.Length];
        }

        /// <summary>
        /// Map entity → DTO response.
        /// </summary>
        private static TicketTypeResponse MapToResponse(TicketType ticketType, string? sectionColor = null)
        {
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
                SectionColor = sectionColor,
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

            var sectionColor = await _context.VenueSectionLayouts
                .Where(l => l.TicketTypeId == ticketType.Id)
                .Select(l => l.Color)
                .FirstOrDefaultAsync();

            return MapToResponse(ticketType, sectionColor);
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

            var sectionColor = await _context.VenueSectionLayouts
                .Where(l => l.TicketTypeId == ticketType.Id)
                .Select(l => l.Color)
                .FirstOrDefaultAsync();

            return MapToResponse(ticketType, sectionColor);
        }

        public async Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByEventIdAsync(Guid eventId, PaginationRequest<TicketTypeResponse> request)
        {
            var eventExist = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventExist == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            // Lấy màu từ VenueSectionLayout theo TicketTypeId
            var colorDict = await _context.VenueSectionLayouts
                .Where(l => l.TicketTypeId != null)
                .Select(l => new { l.TicketTypeId, l.Color })
                .ToListAsync();

            var colorMap = colorDict
                .Where(x => x.TicketTypeId.HasValue)
                .ToDictionary(x => x.TicketTypeId!.Value, x => x.Color);

            var query = _context.TicketTypes
                .Where(tt => tt.EventId == eventId)
                .OrderBy(tt => tt.Price)
                .AsQueryable();

            var projected = query.Select(tt => new TicketTypeResponse
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
                SectionColor = colorMap.ContainsKey(tt.Id) ? colorMap[tt.Id] : null,
                Status = tt.Status,
                SaleStartTime = tt.SaleStartTime,
                SaleEndTime = tt.SaleEndTime,
                IsSeatRequired = tt.IsSeatRequired
            });

            return await projected.GetPaged(request.CurrentPage, request.PageSize);
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

            // Lấy màu từ VenueSectionLayout theo TicketTypeId
            var colorDict = await _context.VenueSectionLayouts
                .Where(l => l.TicketTypeId != null)
                .Select(l => new { l.TicketTypeId, l.Color })
                .ToListAsync();

            var colorMap = colorDict
                .Where(x => x.TicketTypeId.HasValue)
                .ToDictionary(x => x.TicketTypeId!.Value, x => x.Color);

            var query = _context.TicketTypes
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
                    Section = tt.Section,
                    SectionColor = colorMap.ContainsKey(tt.Id) ? colorMap[tt.Id] : null,
                    Status = tt.Status,
                    SaleStartTime = tt.SaleStartTime,
                    SaleEndTime = tt.SaleEndTime,
                    IsSeatRequired = tt.IsSeatRequired,
                    CreatedAt = tt.CreatedAt,
                    CreatedBy = tt.CreatedBy,
                    UpdatedAt = tt.UpdatedAt,
                    UpdatedBy = tt.UpdatedBy
                });

            return await query.GetPaged(request.CurrentPage, request.PageSize);
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

            if (request.SaleStartTime >= request.SaleEndTime)
                throw new BadRequestException(SystemError.INVALID_SALE_TIME);

            if (request.SaleEndTime > ticketType.Event.StartTime)
                throw new BadRequestException(SystemError.SALE_END_TIME_INVALID);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var oldQuantity = ticketType.Quantity;
                var quantityChanged = request.Quantity != oldQuantity;

                ticketType.Name = request.Name.Trim();
                ticketType.Description = request.Description;
                ticketType.Price = request.Price;
                ticketType.Quantity = request.Quantity;
                ticketType.SaleStartTime = request.SaleStartTime;
                ticketType.SaleEndTime = request.SaleEndTime;
                ticketType.UpdatedAt = DateTime.UtcNow;
                ticketType.UpdatedBy = userId;

                await _context.SaveChangesAsync();

                // Nếu loại vé có ghế và quantity tăng → generate thêm ghế
                if (ticketType.IsSeatRequired &&
                    quantityChanged &&
                    request.Quantity > oldQuantity)
                {
                    var additionalSeats = request.Quantity - oldQuantity;
                    await GenerateAdditionalSeatsForTicketTypeAsync(
                        ticketType,
                        ticketType.Event.VenueId,
                        additionalSeats);
                }

                // Cập nhật màu nếu được cung cấp
                if (!string.IsNullOrWhiteSpace(request.SectionColor))
                {
                    await UpsertSectionLayoutAsync(
                        ticketType.Event.VenueId,
                        ticketType.Id,
                        ticketType.Section ?? ticketType.Name,
                        request.SectionColor);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Lấy màu hiện tại từ layout
                var currentColor = await _context.VenueSectionLayouts
                    .Where(l => l.TicketTypeId == ticketType.Id)
                    .Select(l => l.Color)
                    .FirstOrDefaultAsync();

                return MapToResponse(ticketType, currentColor);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Generate thêm ghế khi tăng quantity của một ticket type đã có ghế.
        /// Tiếp nối row/number từ các ghế đã tồn tại.
        /// </summary>
        private async Task GenerateAdditionalSeatsForTicketTypeAsync(
            TicketType ticketType,
            Guid venueId,
            int additionalCount)
        {
            var sectionName = ticketType.Section ?? ticketType.Name;

            var existingCount = await _context.Seats
                .CountAsync(s => s.Section == sectionName && s.VenueId == venueId);

            var totalSeats = existingCount + additionalCount;
            var cols = (int)Math.Ceiling(Math.Sqrt(totalSeats));

            const decimal startX = 20m;
            const decimal startY = 20m;
            const decimal gapX = 40m;
            const decimal gapY = 40m;

            var newSeats = new List<Seat>();

            for (int i = existingCount; i < totalSeats; i++)
            {
                var r = i / cols;
                var c = i % cols;
                var rowLabel = SeatHelper.IndexToRowLabel(r);
                var seatNumber = (c + 1).ToString();

                newSeats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    VenueId = venueId,
                    VenueZoneId = null,
                    Section = sectionName,
                    Row = rowLabel,
                    Number = seatNumber,
                    Xposition = startX + (c * gapX),
                    Yposition = startY + (r * gapY),
                    Status = SeatStatus.AVAILABLE
                });
            }

            await _context.Seats.AddRangeAsync(newSeats);
            await _context.SaveChangesAsync();

            var newEventSeatStatuses = newSeats.Select(seat => new EventSeatStatus
            {
                Id = Guid.NewGuid(),
                EventId = ticketType.EventId,
                SeatId = seat.Id,
                TicketTypeId = ticketType.Id,
                Status = SeatStatus.AVAILABLE
            });

            await _context.EventSeatStatuses.AddRangeAsync(newEventSeatStatuses);
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

            var sectionColor = await _context.VenueSectionLayouts
                .Where(l => l.TicketTypeId == ticketType.Id)
                .Select(l => l.Color)
                .FirstOrDefaultAsync();

            return MapToResponse(ticketType, sectionColor);
        }
    }
}
