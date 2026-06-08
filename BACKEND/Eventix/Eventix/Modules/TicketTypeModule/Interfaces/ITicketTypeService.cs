using Eventix.Common.Models;
using Eventix.Modules.TicketTypeModule.DTOs;

namespace Eventix.Modules.TicketTypeModule.Interfaces
{
    public interface ITicketTypeService
    {
        Task<TicketTypeResponse> CreateTicketTypeAsync(Guid eventId, CreateTicketTypeRequest request, Guid userId);
        Task<TicketTypeResponse> GetTicketTypeByIdAsync(Guid id);
        Task<PaginationResponse<TicketTypeResponse>> GetTicketTypesByEventIdAsync(Guid eventId, PaginationRequest<TicketTypeResponse> request);
        Task<TicketTypeResponse> UpdateTicketTypeAsync(Guid id, UpdateTicketTypeRequest request, Guid userId);
        Task DeleteTicketTypeAsync(Guid id);
    }
}
