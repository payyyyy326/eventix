using Eventix.Share.Commerce;

namespace Eventix.Modules.CommerceModule.Interfaces;

public interface ICommerceService
{
    Task<OrderResponse> CreateOrderAsync(
        IReadOnlyCollection<Guid> reservationIds,
        Guid userId);
    Task<List<OrderResponse>> GetMyOrdersAsync(Guid userId);
    Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId);
    Task<PaymentResponse> CompleteDemoPaymentAsync(Guid orderId, Guid userId);
    Task<List<TicketResponse>> GetMyTicketsAsync(Guid userId);
    Task<TicketResponse> GetTicketAsync(Guid ticketId, Guid userId);
    Task<List<TicketResponse>> GetEventTicketsAsync(
        Guid eventId, Guid staffUserId, bool isAdmin);
    Task<CheckInResponse> CheckInAsync(CheckInRequest request, Guid staffUserId, bool isAdmin);
    Task<CheckInStatsResponse> GetCheckInStatsAsync(Guid eventId, Guid staffUserId, bool isAdmin);
}
