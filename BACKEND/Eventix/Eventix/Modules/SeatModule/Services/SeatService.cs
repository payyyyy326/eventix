using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Helpers;
using Eventix.Modules.SeatModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Seat;
using Eventix.Share.SeatMap;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Eventix.Modules.SeatModule.Services
{
    public class SeatService : ISeatService
    {
        private readonly AppDbContext _context;

        public SeatService(AppDbContext context)
        {
            _context = context;
        }
        public byte[] GenerateSeatTemplateExcel()
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("SeatTemplate");

            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);

            var headers = new[]
            {
                "Section",
                "StartRow",
                "EndRow",
                "StartNumber",
                "EndNumber",
                "StartX",
                "StartY",
                "Gap"
            };

            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(i, 18 * 256);
            }

            using var memoryStream = new MemoryStream();
            workbook.Write(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<PaginationResponse<SeatResponse>> GetSeatsByVenueAsync(Guid venueId, PaginationRequest<SeatResponse> request)
        {
            var venue = await _context.Venues.FindAsync(venueId);
            if (venue == null) throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            var seats = _context.Seats
                .Where(s => s.VenueId == venueId)
                .AsNoTracking()
                .OrderBy(s => s.Section)
                .ThenBy(s => s.Row)
                .ThenBy(s => Convert.ToInt32(s.Number))
                .Select(s => new SeatResponse
                {
                    Id = s.Id,
                    VenueId = s.VenueId,
                    Section = s.Section,
                    Row = s.Row,
                    Number = s.Number,
                    Xposition = s.Xposition,
                    Yposition = s.Yposition,
                    Status = s.Status
                });

            var response = await seats.GetPaged(request.CurrentPage, request.PageSize);

            return response;
        }

        public async Task<ImportSeatResult> ImportSeatByExcelAsync(Guid venueId, ImportSeatsRequest request)
        {
            if (request.File == null || request.File.Length == 0) throw new BadRequestException(SystemError.INVALID_FORMAT);

            var venue = await _context.Venues.FindAsync(venueId);
            if (venue == null) throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            using var stream = request.File.OpenReadStream();
            IWorkbook workbook = new XSSFWorkbook(stream);
            ISheet sheet = workbook.GetSheetAt(0);

            using var transaction = await _context.Database.BeginTransactionAsync();

            var result = new ImportSeatResult
            {
                TotalRows = 0,
                CreatedCount = 0,
                UpdatedCount = 0,
                FailedCount = 0,
                Errors = new List<string>()
            };

            try
            {
                var existingSeats = await _context.Seats
                    .Where(s => s.VenueId == venueId)
                    .ToListAsync();

                var existingSeatDict = existingSeats
                    .ToDictionary(
                        s => SeatHelper.BuildSeatKey(s.Section, s.Row, s.Number),
                        s => s
                    );

                var fileSeatKeys = new HashSet<string>();
                var newSeats = new List<Seat>();
                var zoneDict = await _context.VenueZones
                    .Where(z => z.VenueId == venueId)
                    .ToDictionaryAsync(z => z.Name, z => z);

                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);

                    if (row == null)
                        continue;

                    result.TotalRows++;

                    int excelRowNumber = rowIndex + 1;

                    var section = ExcelHelper.GetCellValue(row, 0);
                    var startRowText = ExcelHelper.GetCellValue(row, 1);
                    var endRowText = ExcelHelper.GetCellValue(row, 2);
                    var startNumberText = ExcelHelper.GetCellValue(row, 3);
                    var endNumberText = ExcelHelper.GetCellValue(row, 4);
                    var startXText = ExcelHelper.GetCellValue(row, 5);
                    var startYText = ExcelHelper.GetCellValue(row, 6);
                    var gapXText = ExcelHelper.GetCellValue(row, 7);
                    var gapYText = ExcelHelper.GetCellValue(row, 7);
                    var status = SystemConstants.SeatStatus.AVAILABLE;

                    var sectionName = section?.Trim();

                    if (string.IsNullOrWhiteSpace(sectionName))
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: Section không được để trống.");
                        continue;
                    }

                    if (!zoneDict.TryGetValue(sectionName, out var zone))
                    {
                        zone = new VenueZone
                        {
                            Id = Guid.NewGuid(),
                            VenueId = venueId,
                            Name = sectionName,
                            HasSeats = true,
                            Capacity = 0,
                            Color = "#60A5FA",
                            SortOrder = zoneDict.Count + 1,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.VenueZones.AddAsync(zone);
                        zoneDict.Add(sectionName, zone);
                    }

                    var startRow = startRowText?.Trim().ToUpper();
                    var endRow = endRowText?.Trim().ToUpper();

                    if (string.IsNullOrWhiteSpace(startRow) ||
                        string.IsNullOrWhiteSpace(endRow) ||
                        !SeatHelper.IsValidRowLabel(startRow) ||
                        !SeatHelper.IsValidRowLabel(endRow) ||
                        !int.TryParse(startNumberText, out var startNumber) ||
                        !int.TryParse(endNumberText, out var endNumber))
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: StartRow, EndRow phải là chữ cái. StartNumber, EndNumber phải là số nguyên.");
                        continue;
                    }

                    var startRowIndex = SeatHelper.RowLabelToIndex(startRow);
                    var endRowIndex = SeatHelper.RowLabelToIndex(endRow);

                    if (startNumber <= 0 || endNumber <= 0)
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: StartNumber và EndNumber phải lớn hơn 0.");
                        continue;
                    }

                    if (startRowIndex > endRowIndex)
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: StartRow không được lớn hơn EndRow.");
                        continue;
                    }

                    if (startNumber > endNumber)
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: StartNumber không được lớn hơn EndNumber.");
                        continue;
                    }

                    if (!decimal.TryParse(startXText, out var startX) ||
                        !decimal.TryParse(startYText, out var startY) ||
                        !decimal.TryParse(gapXText, out var gapX) ||
                        !decimal.TryParse(gapYText, out var gapY))
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: StartX, StartY, GapX, GapY phải là số.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(status))
                    {
                        status = "Available";
                    }

                    for (int r = startRowIndex; r <= endRowIndex; r++)
                    {
                        var seatRow = SeatHelper.IndexToRowLabel(r);

                        for (int n = startNumber; n <= endNumber; n++)
                        {
                            var seatNumber = n.ToString();

                            var seatKey = SeatHelper.BuildSeatKey(sectionName, seatRow, seatNumber);

                            if (!fileSeatKeys.Add(seatKey))
                            {
                                result.Errors.Add($"Dòng {excelRowNumber}: Ghế {section}-{seatRow}-{seatNumber} bị trùng trong file.");
                                continue;
                            }

                            var xPosition = startX + ((n - startNumber) * gapX);
                            var yPosition = startY + ((r - startRowIndex) * gapY);

                            if (existingSeatDict.TryGetValue(seatKey, out var existingSeat))
                            {
                                if (!request.OverrideExisting)
                                {
                                    result.Errors.Add($"Dòng {excelRowNumber}: Ghế {section}-{seatRow}-{seatNumber} đã tồn tại.");
                                    continue;
                                }

                                existingSeat.VenueZoneId = zone.Id;
                                existingSeat.Section = sectionName;
                                existingSeat.Xposition = xPosition;
                                existingSeat.Yposition = yPosition;
                                existingSeat.Status = status;

                                result.UpdatedCount++;
                            }
                            else
                            {
                                newSeats.Add(new Seat
                                {
                                    Id = Guid.NewGuid(),
                                    VenueId = venueId,
                                    VenueZoneId = zone.Id,
                                    Section = sectionName,
                                    Row = seatRow,
                                    Number = seatNumber,
                                    Xposition = xPosition,
                                    Yposition = yPosition,
                                    Status = status
                                });

                                result.CreatedCount++;
                            }
                        }
                    }
                }

                if (newSeats.Any())
                {
                    await _context.Seats.AddRangeAsync(newSeats);
                }

                result.FailedCount = result.Errors.Count;

                var affectedZoneIds = newSeats
                    .Where(s => s.VenueZoneId.HasValue)
                    .Select(s => s.VenueZoneId!.Value)
                    .Concat(existingSeats
                        .Where(s => s.VenueZoneId.HasValue)
                        .Select(s => s.VenueZoneId!.Value))
                    .Distinct()
                    .ToList();

                foreach (var zoneId in affectedZoneIds)
                {
                    var count = await _context.Seats.CountAsync(s => s.VenueZoneId == zoneId);
                    var zone = await _context.VenueZones.FirstAsync(z => z.Id == zoneId);

                    zone.HasSeats = true;
                    zone.Capacity = count;
                    zone.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SeatSectionResponse>> GetSectionsByVenueAsync(Guid venueId)
        {
            return await _context.Seats
                .Where(s => s.VenueId == venueId && !string.IsNullOrWhiteSpace(s.Section))
                .GroupBy(s => s.Section!)
                .Select(g => new SeatSectionResponse
                {
                    Section = g.Key,
                    SeatCount = g.Count()
                })
                .OrderBy(x => x.Section)
                .ToListAsync();
        }

        public async Task<List<TicketTypeSeatStatusResponse>> GetSeatAssignmentStatusByEventAsync(Guid eventId)
        {
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            // Lấy tất cả TicketType của event
            var ticketTypes = await _context.TicketTypes
                .AsNoTracking()
                .Where(tt => tt.EventId == eventId)
                .ToListAsync();

            // Đếm số ghế đã generate cho từng TicketType
            var generatedCounts = await _context.EventSeatStatuses
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .GroupBy(s => s.TicketTypeId)
                .Select(g => new { TicketTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TicketTypeId, x => x.Count);

            // Lấy màu từ VenueSectionLayout
            var colorMap = await _context.VenueSectionLayouts
                .AsNoTracking()
                .Where(l => l.TicketTypeId != null && l.VenueId == eventEntity.VenueId)
                .ToDictionaryAsync(l => l.TicketTypeId!.Value, l => l.Color);

            return ticketTypes.Select(tt => new TicketTypeSeatStatusResponse
            {
                TicketTypeId   = tt.Id,
                TicketTypeName = tt.Name,
                IsSeatRequired = tt.IsSeatRequired,
                Quantity       = tt.Quantity,
                GeneratedSeats = generatedCounts.TryGetValue(tt.Id, out var c) ? c : 0,
                SectionColor   = colorMap.TryGetValue(tt.Id, out var col) ? col : null
            }).ToList();
        }

        public async Task<ImportSeatResult> GenerateSeatsAsync(Guid venueId, GenerateSeatsRequest request)
        {
            // Chỉ hỗ trợ TicketType-based generate
            if (!request.TicketTypeId.HasValue || request.TicketTypeId.Value == Guid.Empty)
                throw new BadRequestException(SystemError.INVALID_DATA);

            return await GenerateSeatsByTicketTypeAsync(venueId, request);
        }

        /// <summary>
        /// Generate seats theo TicketType (luồng mới).
        /// Seats gắn với Venue, Section = TicketType.Name, VenueZoneId = null.
        /// Tự động tạo EventSeatStatus cho event của ticketType đó.
        /// </summary>
        private async Task<ImportSeatResult> GenerateSeatsByTicketTypeAsync(Guid venueId, GenerateSeatsRequest request)
        {
            var result = new ImportSeatResult
            {
                TotalRows = 0,
                CreatedCount = 0,
                UpdatedCount = 0,
                FailedCount = 0,
                Errors = new List<string>()
            };

            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            var ticketType = await _context.TicketTypes
                .FirstOrDefaultAsync(tt => tt.Id == request.TicketTypeId!.Value);

            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (!ticketType.IsSeatRequired)
                throw new BadRequestException(SystemError.INVALID_DATA);

            var startRow = request.StartRow.Trim().ToUpper();
            var endRow = request.EndRow.Trim().ToUpper();

            if (!SeatHelper.IsValidRowLabel(startRow) || !SeatHelper.IsValidRowLabel(endRow))
                throw new BadRequestException(SystemError.INVALID_FORMAT);

            var startRowIndex = SeatHelper.RowLabelToIndex(startRow);
            var endRowIndex = SeatHelper.RowLabelToIndex(endRow);

            if (startRowIndex > endRowIndex ||
                request.StartNumber <= 0 ||
                request.EndNumber <= 0 ||
                request.StartNumber > request.EndNumber)
            {
                throw new BadRequestException(SystemError.INVALID_FORMAT);
            }

            var sectionName = ticketType.Section ?? ticketType.Name;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingSeats = await _context.Seats
                    .Where(s => s.VenueId == venueId && s.Section == sectionName)
                    .ToListAsync();

                var existingSeatDict = existingSeats.ToDictionary(
                    s => SeatHelper.BuildSeatKey(s.Section, s.Row, s.Number),
                    s => s);

                var newSeats = new List<Seat>();

                for (int r = startRowIndex; r <= endRowIndex; r++)
                {
                    var rowLabel = SeatHelper.IndexToRowLabel(r);

                    for (int n = request.StartNumber; n <= request.EndNumber; n++)
                    {
                        result.TotalRows++;
                        var seatNumber = n.ToString();
                        var seatKey = SeatHelper.BuildSeatKey(sectionName, rowLabel, seatNumber);

                        var xPos = request.StartX + ((n - request.StartNumber) * request.GapX);
                        var yPos = request.StartY + ((r - startRowIndex) * request.GapY);

                        if (existingSeatDict.TryGetValue(seatKey, out var existingSeat))
                        {
                            if (!request.OverrideExisting)
                            {
                                result.Errors.Add($"Seat {sectionName}-{rowLabel}-{seatNumber} already exists.");
                                continue;
                            }

                            existingSeat.Xposition = xPos;
                            existingSeat.Yposition = yPos;
                            existingSeat.Status = SystemConstants.SeatStatus.AVAILABLE;
                            result.UpdatedCount++;
                        }
                        else
                        {
                            newSeats.Add(new Seat
                            {
                                Id = Guid.NewGuid(),
                                VenueId = venueId,
                                VenueZoneId = null,
                                Section = sectionName,
                                Row = rowLabel,
                                Number = seatNumber,
                                Xposition = xPos,
                                Yposition = yPos,
                                Status = SystemConstants.SeatStatus.AVAILABLE
                            });
                            result.CreatedCount++;
                        }
                    }
                }

                if (newSeats.Any())
                    await _context.Seats.AddRangeAsync(newSeats);

                await _context.SaveChangesAsync();

                // Tạo EventSeatStatus cho ghế mới
                if (newSeats.Any())
                {
                    var eventSeatStatuses = newSeats.Select(seat => new EventSeatStatus
                    {
                        Id = Guid.NewGuid(),
                        EventId = ticketType.EventId,
                        SeatId = seat.Id,
                        TicketTypeId = ticketType.Id,
                        Status = SystemConstants.SeatStatus.AVAILABLE
                    });

                    await _context.EventSeatStatuses.AddRangeAsync(eventSeatStatuses);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                result.FailedCount = result.Errors.Count;

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<VenueSectionLayoutResponse>> GetSeatMapByEventAsync(Guid eventId)
        {
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            // Lấy tất cả layout thuộc venue của event, chỉ các block có TicketTypeId
            var layouts = await _context.VenueSectionLayouts
                .AsNoTracking()
                .Where(l => l.VenueId == eventEntity.VenueId && l.TicketTypeId != null)
                .Include(l => l.TicketType)
                .ToListAsync();

            var ticketTypeIds = layouts
                .Where(l => l.TicketType != null && l.TicketType.IsSeatRequired)
                .Select(l => l.TicketTypeId!.Value)
                .Distinct()
                .ToList();

            // Đếm tổng ghế theo TicketType trong event
            var totalSeatsMap = await _context.EventSeatStatuses
                .AsNoTracking()
                .Where(s => s.EventId == eventId && ticketTypeIds.Contains(s.TicketTypeId))
                .GroupBy(s => s.TicketTypeId)
                .Select(g => new { TicketTypeId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.TicketTypeId, x => x.Total);

            // Đếm ghế available theo TicketType
            var availableSeatsMap = await _context.EventSeatStatuses
                .AsNoTracking()
                .Where(s =>
                    s.EventId == eventId &&
                    ticketTypeIds.Contains(s.TicketTypeId) &&
                    s.Status == SystemConstants.SeatStatus.AVAILABLE)
                .GroupBy(s => s.TicketTypeId)
                .Select(g => new { TicketTypeId = g.Key, Available = g.Count() })
                .ToDictionaryAsync(x => x.TicketTypeId, x => x.Available);

            return layouts.Select(l =>
            {
                var isSeatRequired = l.TicketType?.IsSeatRequired ?? false;
                var ttId = l.TicketTypeId!.Value;

                return new VenueSectionLayoutResponse
                {
                    Id = l.Id,
                    VenueId = l.VenueId,
                    TicketTypeId = l.TicketTypeId,
                    Section = l.Section,
                    X = l.X,
                    Y = l.Y,
                    Width = l.Width,
                    Height = l.Height,
                    Color = l.Color,
                    IsSeatRequired = isSeatRequired,
                    TotalSeats = isSeatRequired && totalSeatsMap.TryGetValue(ttId, out var total)
                        ? total
                        : null,
                    AvailableSeats = isSeatRequired && availableSeatsMap.TryGetValue(ttId, out var avail)
                        ? avail
                        : null
                };
            }).ToList();
        }

        public async Task<List<SeatWithStatusResponse>> GetSeatsByTicketTypeAsync(Guid eventId, Guid ticketTypeId)
        {
            var ticketType = await _context.TicketTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId && tt.EventId == eventId);

            if (ticketType == null)
                throw new BadRequestException(SystemError.TICKET_TYPE_NOT_FOUND);

            if (!ticketType.IsSeatRequired)
                throw new BadRequestException(SystemError.INVALID_DATA);

            // JOIN Seat + EventSeatStatus để lấy trạng thái ghế trong event này
            var seats = await (
                from ess in _context.EventSeatStatuses
                join seat in _context.Seats on ess.SeatId equals seat.Id
                where ess.EventId == eventId && ess.TicketTypeId == ticketTypeId
                orderby seat.Row, seat.Number
                select new SeatWithStatusResponse
                {
                    SeatId = seat.Id,
                    Section = seat.Section,
                    Row = seat.Row,
                    Number = seat.Number,
                    Xposition = seat.Xposition,
                    Yposition = seat.Yposition,
                    Status = ess.Status
                }
            ).AsNoTracking().ToListAsync();

            return seats;
        }

        /// <summary>
        /// Generate seats theo VenueZone (luồng cũ, giữ lại để tương thích).
        /// </summary>
        private async Task<ImportSeatResult> GenerateSeatsByZoneAsync(Guid venueId, GenerateSeatsRequest request)
        {
            var result = new ImportSeatResult
            {
                TotalRows = 0,
                CreatedCount = 0,
                UpdatedCount = 0,
                FailedCount = 0,
                Errors = new List<string>()
            };

            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            if (!request.VenueZoneId.HasValue || request.VenueZoneId.Value == Guid.Empty)
                throw new BadRequestException(SystemError.INVALID_DATA);

            var zone = await _context.VenueZones
                .FirstOrDefaultAsync(z =>
                    z.Id == request.VenueZoneId.Value &&
                    z.VenueId == venueId);

            if (zone == null)
                throw new BadRequestException(SystemError.INVALID_DATA);

            if (!zone.HasSeats)
                throw new BadRequestException(SystemError.INVALID_DATA);

            var startRow = request.StartRow.Trim().ToUpper();
            var endRow = request.EndRow.Trim().ToUpper();

            if (!SeatHelper.IsValidRowLabel(startRow) ||
                !SeatHelper.IsValidRowLabel(endRow))
            {
                throw new BadRequestException(SystemError.INVALID_FORMAT);
            }

            var startRowIndex = SeatHelper.RowLabelToIndex(startRow);
            var endRowIndex = SeatHelper.RowLabelToIndex(endRow);

            if (startRowIndex > endRowIndex)
                throw new BadRequestException(SystemError.INVALID_FORMAT);

            if (request.StartNumber <= 0 ||
                request.EndNumber <= 0 ||
                request.StartNumber > request.EndNumber)
            {
                throw new BadRequestException(SystemError.INVALID_FORMAT);
            }

            var expectedSeatCount =
                (endRowIndex - startRowIndex + 1) *
                (request.EndNumber - request.StartNumber + 1);

            var otherZonesCapacity = await _context.VenueZones
                .Where(z => z.VenueId == venueId && z.Id != zone.Id)
                .SumAsync(z => z.Capacity);

            var venueCapacity = await _context.Venues
                .Where(v => v.Id == venueId)
                .Select(v => v.Capacity)
                .FirstAsync();

            if (otherZonesCapacity + expectedSeatCount > venueCapacity)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            if (expectedSeatCount > zone.Capacity)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingSeats = await _context.Seats
                    .Where(s => s.VenueId == venueId &&
                                s.VenueZoneId == zone.Id)
                    .ToListAsync();

                var existingSeatDict = existingSeats.ToDictionary(
                    s => SeatHelper.BuildSeatKey(s.Section, s.Row, s.Number),
                    s => s
                );

                var newSeats = new List<Seat>();

                for (int r = startRowIndex; r <= endRowIndex; r++)
                {
                    var rowLabel = SeatHelper.IndexToRowLabel(r);

                    for (int n = request.StartNumber; n <= request.EndNumber; n++)
                    {
                        result.TotalRows++;

                        var seatNumber = n.ToString();

                        var seatKey = SeatHelper.BuildSeatKey(
                            zone.Name,
                            rowLabel,
                            seatNumber);

                        var xPosition = request.StartX +
                            ((n - request.StartNumber) * request.GapX);

                        var yPosition = request.StartY +
                            ((r - startRowIndex) * request.GapY);

                        if (existingSeatDict.TryGetValue(seatKey, out var existingSeat))
                        {
                            if (!request.OverrideExisting)
                            {
                                result.Errors.Add($"Seat {zone.Name}-{rowLabel}-{seatNumber} already exists.");
                                continue;
                            }

                            existingSeat.Xposition = xPosition;
                            existingSeat.Yposition = yPosition;
                            existingSeat.Status = SystemConstants.SeatStatus.AVAILABLE;
                            existingSeat.Section = zone.Name;
                            existingSeat.VenueZoneId = zone.Id;

                            result.UpdatedCount++;
                        }
                        else
                        {
                            newSeats.Add(new Seat
                            {
                                Id = Guid.NewGuid(),
                                VenueId = venueId,
                                VenueZoneId = zone.Id,
                                Section = zone.Name,
                                Row = rowLabel,
                                Number = seatNumber,
                                Xposition = xPosition,
                                Yposition = yPosition,
                                Status = SystemConstants.SeatStatus.AVAILABLE
                            });

                            result.CreatedCount++;
                        }
                    }
                }

                if (newSeats.Any())
                    await _context.Seats.AddRangeAsync(newSeats);

                await _context.SaveChangesAsync();

                zone.Capacity = await _context.Seats
                    .CountAsync(s => s.VenueZoneId == zone.Id);

                zone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                result.FailedCount = result.Errors.Count;

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}