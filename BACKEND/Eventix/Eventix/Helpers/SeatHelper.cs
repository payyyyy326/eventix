namespace Eventix.Helpers
{
    public static class SeatHelper
    {
        public static string BuildSeatKey(
            string? section,
            string? row,
            string number)
        {
            return $"{section?.Trim().ToLower()}|{row?.Trim().ToLower()}|{number.Trim().ToLower()}";
        }
        public static bool IsValidRowLabel(string row)
        {
            return !string.IsNullOrWhiteSpace(row)
                && row.All(char.IsLetter);
        }

        public static int RowLabelToIndex(string row)
        {
            row = row.Trim().ToUpper();

            int result = 0;

            foreach (var c in row)
            {
                result = result * 26 + (c - 'A' + 1);
            }

            return result - 1;
        }

        public static string IndexToRowLabel(int index)
        {
            index++;

            var result = "";

            while (index > 0)
            {
                index--;
                result = (char)('A' + index % 26) + result;
                index /= 26;
            }

            return result;
        }
    }
}
