namespace Eventix.Share.Commerce;

public class OrderResponse
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal SubTotal { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

public class OrderItemResponse
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public string TicketTypeName { get; set; } = "";
    public string? SeatLabel { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
