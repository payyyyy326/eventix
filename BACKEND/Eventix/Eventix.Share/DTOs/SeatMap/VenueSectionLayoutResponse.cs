namespace Eventix.Share.SeatMap
{
    public class VenueSectionLayoutResponse
    {
        public Guid Id { get; set; }

        public Guid VenueId { get; set; }

        /// <summary>
        /// TicketType mà block này đại diện trên map.
        /// </summary>
        public Guid? TicketTypeId { get; set; }

        /// <summary>
        /// Tên loại vé / section.
        /// </summary>
        public string Section { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// True = block này có ghế ngồi (người dùng cần chọn ghế cụ thể).
        /// False = đứng / general admission.
        /// </summary>
        public bool IsSeatRequired { get; set; }

        /// <summary>
        /// Số ghế khả dụng (chỉ có giá trị khi IsSeatRequired = true).
        /// </summary>
        public int? AvailableSeats { get; set; }

        /// <summary>
        /// Tổng số ghế (chỉ có giá trị khi IsSeatRequired = true).
        /// </summary>
        public int? TotalSeats { get; set; }
    }
}
