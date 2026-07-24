namespace Eventix.Share.Seat
{
    public class GenerateSeatsRequest
    {
        /// <summary>
        /// Zone-based generate (legacy). Nếu TicketTypeId được cung cấp, trường này bị bỏ qua.
        /// </summary>
        public Guid? VenueZoneId { get; set; }

        /// <summary>
        /// TicketType-based generate (luồng mới). Khi có giá trị, seats sẽ được gắn theo
        /// TicketType thay vì VenueZone.
        /// </summary>
        public Guid? TicketTypeId { get; set; }

        public string StartRow { get; set; } = string.Empty;

        public string EndRow { get; set; } = string.Empty;

        public int StartNumber { get; set; }

        public int EndNumber { get; set; }

        public decimal StartX { get; set; }

        public decimal StartY { get; set; }

        public decimal GapX { get; set; }

        public decimal GapY { get; set; }

        public bool OverrideExisting { get; set; }
    }
}