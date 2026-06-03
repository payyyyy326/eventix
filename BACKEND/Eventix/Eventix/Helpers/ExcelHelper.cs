using NPOI.SS.UserModel;

namespace Eventix.Helpers
{
    public static class ExcelHelper
    {
        public static string? GetCellValue(IRow row, int cellIndex)
        {
            return row.GetCell(cellIndex)?.ToString()?.Trim();
        }
    }
}
