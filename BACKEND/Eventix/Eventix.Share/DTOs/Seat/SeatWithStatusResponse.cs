namespace Eventix.Share.Seat
{
    /// <summary>
    /// Thông tin ghế kèm trạng thái trong một event cụ thể.
    /// Dùng để hiển thị sơ đồ ghế cho buyer chọn.
    /// </summary>
    public class SeatWithStatusResponse
    {
        public Guid SeatId { get; set; }

        public string? Section { get; set; }

        public string? Row { get; set; }

        public string Number { get; set; } = null!;

        /// <summary>
        /// Label hiển thị: "{Row}-{Number}" (ví dụ: "A-1")
        /// </summary>
        public string Label => $"{Row}-{Number}";

        public decimal? Xposition { get; set; }

        public decimal? Yposition { get; set; }

        /// <summary>
        /// Trạng thái trong event: Available, Reserved, Sold
        /// </summary>
        public string Status { get; set; } = null!;
    }
}
