using Eventix.Share.Common.Models;
using Eventix.Share.TicketType;

namespace Eventix.Modules.TicketTypeModule.Interfaces
{
    public interface ITicketTypeService
    {
        Task<TicketTypeResponse> CreateTicketTypeAsync(Guid eventId, CreateTicketTypeRequest request, Guid userId);
        Task<TicketTypeResponse> GetTicketTypeByIdAsync(Guid id);
        Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByEventIdAsync(Guid eventId, PaginationRequest<TicketTypeResponse> request);
        Task<TicketTypeResponse> UpdateTicketTypeAsync(Guid id, UpdateTicketTypeRequest request, Guid userId);
        Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByOrganizerEventAsync(Guid userId, Guid eventId, PaginationRequest<TicketTypeResponse> request);
        Task<TicketTypeResponse> GetTicketTypeByIdForOrganizerAsync(Guid userId, Guid ticketTypeId);
        Task DeleteTicketTypeAsync(Guid id);
        Task<TicketTypeResponse> DeactivateTicketTypeAsync(Guid id, Guid userId);
    }
}
