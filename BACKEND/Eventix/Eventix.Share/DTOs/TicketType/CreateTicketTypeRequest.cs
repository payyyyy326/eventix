namespace Eventix.Share.TicketType
{
    public class CreateTicketTypeRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public DateTime SaleStartTime { get; set; }

        public DateTime SaleEndTime { get; set; }

        /// <summary>
        /// True = loại vé có ghế ngồi (auto-generate seats theo Quantity).
        /// False = đứng / general admission.
        /// </summary>
        public bool IsSeatRequired { get; set; }

        /// <summary>
        /// Màu hiển thị trên venue map cho loại vé này.
        /// Nếu null, hệ thống sẽ tự gán màu mặc định.
        /// </summary>
        public string? SectionColor { get; set; }
    }
}
