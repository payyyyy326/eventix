using Eventix.Controllers;
using Eventix.Modules.Admin.Interfaces;
using Eventix.Share.Admin;
using Eventix.Share.Common.Constants;
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
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        // ── Users ─────────────────────────────────────────────────────────────

        /// <summary>Danh sách người dùng (phân trang, filter).</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterRequest request)
        {
            var result = await _adminService.GetUsersAsync(request);
            return Ok(result);
        }

        /// <summary>Ban người dùng.</summary>
        [HttpPatch("users/{userId:guid}/ban")]
        public async Task<IActionResult> BanUser(Guid userId, [FromBody] AdminBanUserRequest body)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.BanUserAsync(userId, body.Reason, adminId);
            return Ok(result);
        }

        /// <summary>Unban người dùng.</summary>
        [HttpPatch("users/{userId:guid}/unban")]
        public async Task<IActionResult> UnbanUser(Guid userId)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.UnbanUserAsync(userId, adminId);
            return Ok(result);
        }

        // ── Organizer requests ────────────────────────────────────────────────

        /// <summary>Danh sách đơn đăng ký organizer.</summary>
        [HttpGet("organizer-requests")]
        public async Task<IActionResult> GetOrganizerRequests(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetOrganizerRequestsAsync(status, page, pageSize);
            return Ok(result);
        }

        /// <summary>Chi tiết một organizer.</summary>
        [HttpGet("organizer-requests/{id:guid}")]
        public async Task<IActionResult> GetOrganizerDetail(Guid id)
        {
            var result = await _adminService.GetOrganizerDetailAsync(id);
            return Ok(result);
        }

        /// <summary>Duyệt đơn đăng ký organizer.</summary>
        [HttpPatch("organizer-requests/{id:guid}/approve")]
        public async Task<IActionResult> ApproveOrganizer(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.ApproveOrganizerAsync(id, adminId);
            return Ok(result);
        }

        /// <summary>Từ chối đơn đăng ký organizer.</summary>
        [HttpPatch("organizer-requests/{id:guid}/reject")]
        public async Task<IActionResult> RejectOrganizer(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result  = await _adminService.RejectOrganizerAsync(id, adminId);
            return Ok(result);
        }

        // ── Organizer stats ───────────────────────────────────────────────────

        /// <summary>Thống kê các organizer đã được duyệt.</summary>
        [HttpGet("organizers")]
        public async Task<IActionResult> GetOrganizerStats(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetOrganizerStatsAsync(page, pageSize);
            return Ok(result);
        }
    }
}
