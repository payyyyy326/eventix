namespace Eventix.Share.TicketType
{
    public class UpdateTicketTypeRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public DateTime SaleStartTime { get; set; }

        public DateTime SaleEndTime { get; set; }

        /// <summary>
        /// Màu hiển thị trên venue map. Nếu null, giữ nguyên màu cũ.
        /// </summary>
        public string? SectionColor { get; set; }
    }
}
