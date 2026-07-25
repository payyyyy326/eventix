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
        private readonly ILogger<AdminController> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminController(IHttpClientFactory httpClientFactory, ILogger<AdminController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
                else
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Dashboard API lỗi [{Status}]: {Body}", (int)resp.StatusCode, errBody);
                    ViewBag.ApiError = $"Không thể tải dữ liệu dashboard. [HTTP {(int)resp.StatusCode}]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kết nối API dashboard");
                ViewBag.ApiError = $"Không thể kết nối tới API: {ex.Message}";
            }

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
                else
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Users API lỗi [{Status}]: {Body}", (int)resp.StatusCode, errBody);
                    ViewBag.ApiError = $"Không thể tải danh sách người dùng. [HTTP {(int)resp.StatusCode}]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kết nối API users");
                ViewBag.ApiError = $"Không thể kết nối tới API: {ex.Message}";
            }

            ViewBag.Search   = search;
            ViewBag.Status   = status;
            ViewBag.Role     = role;
            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;

            return View(data ?? new PaginationResponse<AdminUserResponse>());
        }

        // ── Ban / Unban (AJAX) ─────────────────────────────────────────────────

        public class BanUserBody { public Guid UserId { get; set; } public string Reason { get; set; } = ""; }
        public class UnbanUserBody { public Guid UserId { get; set; } }

        [HttpPost]
        public async Task<IActionResult> BanUser([FromBody] BanUserBody body)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsJsonAsync(
                $"api/Admin/users/{body.UserId}/ban",
                new AdminBanUserRequest { Reason = body.Reason });

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi ban người dùng." });
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser([FromBody] UnbanUserBody body)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/users/{body.UserId}/unban", null);

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
                else
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("OrganizerRequests API lỗi [{Status}]: {Body}", (int)resp.StatusCode, errBody);
                    ViewBag.ApiError = $"Không thể tải danh sách đơn đăng ký. [HTTP {(int)resp.StatusCode}]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kết nối API organizer-requests");
                ViewBag.ApiError = $"Không thể kết nối tới API: {ex.Message}";
            }

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
                    ViewBag.PendingCount = body?.Data?.TotalRows ?? 0;
                }
                else
                {
                    ViewBag.PendingCount = 0;
                }
            }
            catch { ViewBag.PendingCount = 0; }

            return View(data ?? new PaginationResponse<AdminOrganizerDetailResponse>());
        }

        // ── Approve / Reject (AJAX) ────────────────────────────────────────────

        public class OrganizerIdBody { public Guid Id { get; set; } }

        [HttpPost]
        public async Task<IActionResult> ApproveOrganizer([FromBody] OrganizerIdBody body)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/organizer-requests/{body.Id}/approve", null);

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            var err = await resp.Content.ReadFromJsonAsync<ApiResponseModel<object>>(_jsonOpts);
            return Json(new { success = false, message = err?.Message ?? "Lỗi khi duyệt." });
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrganizer([FromBody] OrganizerIdBody body)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return Json(new { success = false, message = "Unauthorized" });
            if (!IsAdmin()) return Json(new { success = false, message = "Forbidden" });

            var resp = await client.PatchAsync($"api/Admin/organizer-requests/{body.Id}/reject", null);

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
                else
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Organizers API lỗi [{Status}]: {Body}", (int)resp.StatusCode, errBody);
                    ViewBag.ApiError = $"Không thể tải danh sách organizer. [HTTP {(int)resp.StatusCode}]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kết nối API organizers");
                ViewBag.ApiError = $"Không thể kết nối tới API: {ex.Message}";
            }

            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;

            return View(data ?? new PaginationResponse<AdminOrganizerStatsResponse>());
        }
    }
}
