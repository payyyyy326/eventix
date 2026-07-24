namespace Eventix.Web.Helpers
{
    public static class MediaUrl
    {
        // Base URL của API server - nơi lưu trữ file upload
        private const string ApiBase = "https://localhost:7162";

        /// <summary>
        /// Chuyển relative URL từ API thành absolute URL để hiển thị ảnh.
        /// Nếu url đã là absolute (bắt đầu bằng http), trả về nguyên vẹn.
        /// Nếu null/rỗng, trả về fallback.
        /// </summary>
        public static string Img(string? url, string fallback = "/images/no-image.jpg")
        {
            if (string.IsNullOrWhiteSpace(url))
                return fallback;

            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            // relative path: "/uploads/events/xyz.jpg" → "https://localhost:7162/uploads/events/xyz.jpg"
            return $"{ApiBase}{url}";
        }
    }
}
