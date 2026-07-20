using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Eventix.Common.Helpers
{
    public static class SlugHelper
    {
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim().ToLowerInvariant();

            // Bỏ dấu tiếng Việt
            var normalized = text.Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            text = builder.ToString()
                .Normalize(NormalizationForm.FormC);

            text = text.Replace('đ', 'd');

            // Chỉ giữ a-z, 0-9
            text = Regex.Replace(text, @"[^a-z0-9]+", "-");

            // Xóa dấu -
            text = Regex.Replace(text, @"-+", "-");

            return text.Trim('-');
        }
    }
}