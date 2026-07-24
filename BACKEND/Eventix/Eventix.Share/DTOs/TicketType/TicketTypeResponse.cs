namespace Eventix.Share.TicketType
{
    public class TicketTypeResponse
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public int SoldQuantity { get; set; }

        public int ReservedQuantity { get; set; }

        public int AvailableQuantity => Quantity - SoldQuantity - ReservedQuantity;

        public string? Section { get; set; }

        /// <summary>
        /// True = loại vé có ghế ngồi. False = đứng / general admission.
        /// </summary>
        public bool IsSeatRequired { get; set; }

        /// <summary>
        /// Màu hiển thị trên venue map.
        /// </summary>
        public string? SectionColor { get; set; }

        public string Status { get; set; } = null!;

        public DateTime SaleStartTime { get; set; }

        public DateTime SaleEndTime { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }
    }
}
