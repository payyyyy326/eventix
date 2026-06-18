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

                    if (string.IsNullOrWhiteSpace(section))
                    {
                        result.Errors.Add($"Dòng {excelRowNumber}: Section không được để trống.");
                        continue;
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

                            var seatKey = SeatHelper.BuildSeatKey(section, seatRow, seatNumber);

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
                                    Section = section,
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
    }
}
