namespace Eventix.Modules.TicketTypeModule.DTOs
{
    public class CreateTicketTypeRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? Section { get; set; }

        public DateTime SaleStartTime { get; set; }

        public DateTime SaleEndTime { get; set; }

        public bool IsSeatRequired { get; set; }

    }
}
