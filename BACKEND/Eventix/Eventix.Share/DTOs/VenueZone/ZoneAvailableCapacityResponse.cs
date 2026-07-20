namespace Eventix.Share.VenueZone
{
    /// <summary>
    /// Trả về số slot còn trống trong từng zone của một event cụ thể.
    /// "Còn trống" = Capacity của zone - tổng Quantity của tất cả TicketType
    /// đang active trong zone đó cho event này.
    /// </summary>
    public class ZoneAvailableCapacityResponse
    {
        public Guid VenueZoneId { get; set; }

        public string ZoneName { get; set; } = string.Empty;

        public bool HasSeats { get; set; }

        /// <summary>Tổng sức chứa của zone.</summary>
        public int Capacity { get; set; }

        /// <summary>Tổng số vé đã được assign cho zone trong event này.</summary>
        public int AllocatedQuantity { get; set; }

        /// <summary>Số slot còn trống = Capacity - AllocatedQuantity.</summary>
        public int AvailableSlots { get; set; }
    }
}
