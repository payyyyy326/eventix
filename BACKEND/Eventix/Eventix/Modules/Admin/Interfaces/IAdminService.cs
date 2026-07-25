using Eventix.Share.Admin;
using Eventix.Share.Common.Models;

namespace Eventix.Modules.Admin.Interfaces
{
    public interface IAdminService
    {
        // ── Users ─────────────────────────────────────────────────────────────
        Task<PaginationResponse<AdminUserResponse>> GetUsersAsync(AdminUserFilterRequest request);
        Task<AdminUserResponse> BanUserAsync(Guid userId, string reason, Guid adminId);
        Task<AdminUserResponse> UnbanUserAsync(Guid userId, Guid adminId);

        // ── Organizer requests ────────────────────────────────────────────────
        Task<PaginationResponse<AdminOrganizerDetailResponse>> GetOrganizerRequestsAsync(string? status, int page, int pageSize);
        Task<AdminOrganizerDetailResponse> GetOrganizerDetailAsync(Guid organizerProfileId);
        Task<AdminOrganizerDetailResponse> ApproveOrganizerAsync(Guid organizerProfileId, Guid adminId);
        Task<AdminOrganizerDetailResponse> RejectOrganizerAsync(Guid organizerProfileId, Guid adminId);

        // ── Organizer stats list ──────────────────────────────────────────────
        Task<PaginationResponse<AdminOrganizerStatsResponse>> GetOrganizerStatsAsync(int page, int pageSize);

        // ── Dashboard ─────────────────────────────────────────────────────────
        Task<AdminDashboardStats> GetDashboardStatsAsync();
    }
}
