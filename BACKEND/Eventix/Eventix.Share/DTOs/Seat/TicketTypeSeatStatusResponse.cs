namespace Eventix.Share.Seat
{
    /// <summary>
    /// Trạng thái generate seat của một TicketType trong event.
    /// Thay thế SeatImportStatusResponse (zone-based) bằng TicketType-based.
    /// </summary>
    public class TicketTypeSeatStatusResponse
    {
        public Guid TicketTypeId { get; set; }

        public string TicketTypeName { get; set; } = string.Empty;

        /// <summary>True = loại vé có ghế ngồi và cần generate seats.</summary>
        public bool IsSeatRequired { get; set; }

        /// <summary>Số lượng vé (= số ghế cần có).</summary>
        public int Quantity { get; set; }

        /// <summary>Số ghế đã được generate trong event này.</summary>
        public int GeneratedSeats { get; set; }

        /// <summary>
        /// True nếu không cần ghế (IsSeatRequired = false)
        /// hoặc đã generate đủ số ghế theo Quantity.
        /// </summary>
        public bool Completed => !IsSeatRequired || GeneratedSeats >= Quantity;

        public string? SectionColor { get; set; }
    }
}
