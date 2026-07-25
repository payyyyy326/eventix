using Eventix.Controllers;
using Eventix.Common.Constants.SystemData;
using Eventix.Modules.Admin.Interfaces;
using Eventix.Share.Admin;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SystemConstants.RoleConstants.ADMIN)]
    public class AdminController : BaseApiController
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        /// <summary>Thống kê tổng quan hệ thống.</summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponseModel<AdminDashboardStats>>> GetDashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return SuccessResponse(SystemSuccess.SUCCESS, stats);
        }

        // ── Users ─────────────────────────────────────────────────────────────

        /// <summary>Danh sách người dùng (phân trang, filter).</summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<AdminUserResponse>>>> GetUsers([FromQuery] AdminUserFilterRequest request)
        {
            var result = await _adminService.GetUsersAsync(request);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>Ban người dùng.</summary>
        [HttpPatch("users/{userId:guid}/ban")]
        public async Task<ActionResult<ApiResponseModel<AdminUserResponse>>> BanUser(Guid userId, [FromBody] AdminBanUserRequest body)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.BanUserAsync(userId, body.Reason, adminId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>Unban người dùng.</summary>
        [HttpPatch("users/{userId:guid}/unban")]
        public async Task<ActionResult<ApiResponseModel<AdminUserResponse>>> UnbanUser(Guid userId)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.UnbanUserAsync(userId, adminId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        // ── Organizer requests ────────────────────────────────────────────────

        /// <summary>Danh sách đơn đăng ký organizer.</summary>
        [HttpGet("organizer-requests")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<AdminOrganizerDetailResponse>>>> GetOrganizerRequests(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetOrganizerRequestsAsync(status, page, pageSize);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>Chi tiết một organizer.</summary>
        [HttpGet("organizer-requests/{id:guid}")]
        public async Task<ActionResult<ApiResponseModel<AdminOrganizerDetailResponse>>> GetOrganizerDetail(Guid id)
        {
            var result = await _adminService.GetOrganizerDetailAsync(id);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>Duyệt đơn đăng ký organizer.</summary>
        [HttpPatch("organizer-requests/{id:guid}/approve")]
        public async Task<ActionResult<ApiResponseModel<AdminOrganizerDetailResponse>>> ApproveOrganizer(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.ApproveOrganizerAsync(id, adminId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        /// <summary>Từ chối đơn đăng ký organizer.</summary>
        [HttpPatch("organizer-requests/{id:guid}/reject")]
        public async Task<ActionResult<ApiResponseModel<AdminOrganizerDetailResponse>>> RejectOrganizer(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.RejectOrganizerAsync(id, adminId);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }

        // ── Organizer stats ───────────────────────────────────────────────────

        /// <summary>Thống kê các organizer đã được duyệt.</summary>
        [HttpGet("organizers")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<AdminOrganizerStatsResponse>>>> GetOrganizerStats(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetOrganizerStatsAsync(page, pageSize);
            return SuccessResponse(SystemSuccess.SUCCESS, result);
        }
    }
}
