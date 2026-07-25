using Eventix.Share.Admin;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Eventix.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ── Auth helper ───────────────────────────────────────────────────────

        private HttpClient? CreateAuthorizedClient()
        {
            var token = Request.Cookies[SystemConstants.CookieNames.Token];
            if (string.IsNullOrWhiteSpace(token)) return null;

            var client = _httpClientFactory.CreateClient("Eventix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private bool IsAdmin()
        {
            var roles = Request.Cookies[SystemConstants.CookieNames.Roles] ?? "";
            return roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        }

        private IActionResult RequireAdminLogin()
        {
            TempData["Error"] = "Bạn không có quyền truy cập trang Admin.";
            return RedirectToAction("Login", "Auth");
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");
            if (!IsAdmin()) return RequireAdminLogin();

            AdminDashboardStats? stats = null;
            try
            {
                var resp = await client.GetAsync("api/Admin/dashboard");
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadFromJsonAsync<ApiResponseModel<AdminDashboardStats>>(_jsonOpts);
                    stats = body?.Data;
                }
            }
            catch { /* use defaults */ }

            return View(stats ?? new AdminDashboardStats());
        }

        // ── Users ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Users(
            string? search, string? status, string? role, int page = 1, int pageSize = 20)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");
            if (!IsAdmin()) return RequireAdminLogin();

            var qs = $"api/Admin/users?CurrentPage={page}&PageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))  qs += $"&Search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(status))  qs += $"&Status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrWhiteSpace(role))    qs += $"&Role={Uri.EscapeDataString(role)}";

            PaginationResponse<AdminUserResponse>? data = null;
            try
            {
                var resp = await client.GetAsync(qs);
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content
                        .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<AdminUserResponse>>>(_jsonOpts);
                    data = body?.Data;
                }
            }
            catch { /* empty list */ }

            ViewBag.Search   = search;
            ViewBag.Status   = status;
            ViewBag.Role     = role;
            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;

            return View(data ?? new PaginationResponse<AdminUserResponse>());
        }

        // ── Ban / Unban (AJAX) ─────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> BanUser(Guid userId, string reason = "")
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsJsonAsync(
                $"api/Admin/users/{userId}/ban",
                new AdminBanUserRequest { Reason = reason });

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi ban người dùng." });
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(Guid userId)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/users/{userId}/unban", null);

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi unban người dùng." });
        }

        // ── Organizer Requests ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> OrganizerRequests(
            string? status, int page = 1, int pageSize = 20)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");
            if (!IsAdmin()) return RequireAdminLogin();

            var qs = $"api/Admin/organizer-requests?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={Uri.EscapeDataString(status)}";

            PaginationResponse<AdminOrganizerDetailResponse>? data = null;
            try
            {
                var resp = await client.GetAsync(qs);
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content
                        .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<AdminOrganizerDetailResponse>>>(_jsonOpts);
                    data = body?.Data;
                }
            }
            catch { /* empty */ }

            ViewBag.Status   = status;
            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;

            // Pending count cho badge
            try
            {
                var countResp = await client.GetAsync("api/Admin/organizer-requests?status=Pending&page=1&pageSize=1");
                if (countResp.IsSuccessStatusCode)
                {
                    var body = await countResp.Content
                        .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<AdminOrganizerDetailResponse>>>(_jsonOpts);
                    ViewBag.PendingCount = body?.Data?.TotalCount ?? 0;
                }
            }
            catch { ViewBag.PendingCount = 0; }

            return View(data ?? new PaginationResponse<AdminOrganizerDetailResponse>());
        }

        // ── Approve / Reject (AJAX) ────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> ApproveOrganizer(Guid id)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/organizer-requests/{id}/approve", null);

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi duyệt." });
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrganizer(Guid id)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/organizer-requests/{id}/reject", null);

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi từ chối." });
        }

        // ── Organizers Stats ──────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Organizers(int page = 1, int pageSize = 20)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");
            if (!IsAdmin()) return RequireAdminLogin();

            PaginationResponse<AdminOrganizerStatsResponse>? data = null;
            try
            {
                var resp = await client.GetAsync(
                    $"api/Admin/organizers?page={page}&pageSize={pageSize}");
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content
                        .ReadFromJsonAsync<ApiResponseModel<PaginationResponse<AdminOrganizerStatsResponse>>>(_jsonOpts);
                    data = body?.Data;
                }
            }
            catch { /* empty */ }

            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;

            return View(data ?? new PaginationResponse<AdminOrganizerStatsResponse>());
        }
    }
}
