namespace Eventix.Share.Commerce;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Gateway { get; set; } = "";
    public string? TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime? PaidAt { get; set; }
}
