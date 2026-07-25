using Eventix.Share.Common.Models;
using Eventix.Share.Organizer;
using Eventix.Share.User;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Eventix.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateAuthorizedClient()
        {
            var token = Request.Cookies["token"];

            if (string.IsNullOrWhiteSpace(token))
                return null!;

            var client = _httpClientFactory.CreateClient("Eventix");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var client = CreateAuthorizedClient();

            if (client == null)
                return RedirectToAction("Login", "Auth");

            var response = await client.GetAsync("api/user/profile");

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

            if (!response.IsSuccessStatusCode || result?.Data == null)
                return RedirectToAction("Login", "Auth");

            var model = new ProfileViewModel
            {
                Profile = result.Data,
                EditRequest = new UpdateProfileRequest
                {
                    FullName = result.Data.FullName,
                    PhoneNumber = result.Data.PhoneNumber
                }
            };

            return View(model);
        }

        // GET: EditProfile - redirect về Profile với edit mở sẵn
        [HttpGet]
        public IActionResult EditProfile()
        {
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile([Bind(Prefix = "EditRequest")] UpdateProfileRequest model)
        {
            var client = CreateAuthorizedClient();

            if (client == null)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                // Tải lại profile data để render trang Profile với lỗi
                var profileResp = await client.GetAsync("api/user/profile");
                var profileResult = await profileResp.Content
                    .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

                var profileModel = new ProfileViewModel
                {
                    Profile = profileResult?.Data ?? new UserResponse(),
                    EditRequest = model
                };
                ViewBag.ShowEdit = true;
                return View("Profile", profileModel);
            }

            var response = await client.PutAsJsonAsync("api/user/profile", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                // Tải lại profile data để render trang Profile với lỗi
                var profileResp = await client.GetAsync("api/user/profile");
                var profileResult = await profileResp.Content
                    .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

                var profileModel = new ProfileViewModel
                {
                    Profile = profileResult?.Data ?? new UserResponse(),
                    EditRequest = model
                };
                ModelState.AddModelError("", result?.Message ?? "Cập nhật hồ sơ thất bại.");
                ViewBag.ShowEdit = true;
                return View("Profile", profileModel);
            }

            if (result.Data != null)
            {
                Response.Cookies.Append("userName", result.Data.FullName ?? result.Data.Email);
                Response.Cookies.Append("avatarUrl", result.Data.AvatarUrl ?? "/images/default-avatar.png");
            }

            TempData["Success"] = result.Message ?? "Cập nhật hồ sơ thành công.";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                TempData["Error"] = "Please select an image";
                return RedirectToAction("Profile");
            }

            var client = CreateAuthorizedClient();

            if (client == null)
                return RedirectToAction("Login", "Auth");

            using var content = new MultipartFormDataContent();

            using var stream = avatar.OpenReadStream();

            content.Add(
                new StreamContent(stream),
                "Avatar",
                avatar.FileName);

            var response = await client.PutAsync("api/user/avatar", content);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                TempData["Error"] = result?.Message ?? "Upload avatar failed";
                return RedirectToAction("Profile");
            }

            if (result.Data != null)
            {
                Response.Cookies.Append(
                    "avatarUrl",
                    result.Data.AvatarUrl ?? "/images/default-avatar.png");
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("Profile");
        }

        // ── Đăng ký Organizer ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> RegisterOrganizer()
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            // Kiểm tra user đã có hồ sơ organizer chưa
            try
            {
                var check = await client.GetAsync("api/OrganizerProfile/organizer-detail");
                if (check.IsSuccessStatusCode)
                {
                    var existing = await check.Content
                        .ReadFromJsonAsync<ApiResponseModel<OrganizerProfileResponse>>();
                    if (existing?.Data != null)
                    {
                        // Đã có hồ sơ → redirect về trang trạng thái
                        TempData["Info"] = "Bạn đã nộp đơn đăng ký Organizer trước đó.";
                        return RedirectToAction("OrganizerStatus");
                    }
                }
            }
            catch { /* chưa có hồ sơ → cho phép đăng ký */ }

            return View(new CreateOrganizerProfileRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterOrganizer(CreateOrganizerProfileRequest model)
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var response = await client.PostAsJsonAsync("api/OrganizerProfile/create", model);
            var result   = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<OrganizerProfileResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Không thể gửi đơn đăng ký.");
                return View(model);
            }

            TempData["Success"] = "Đơn đăng ký Organizer đã được gửi. Admin sẽ xét duyệt trong thời gian sớm nhất.";
            return RedirectToAction("OrganizerStatus");
        }

        [HttpGet]
        public async Task<IActionResult> OrganizerStatus()
        {
            var client = CreateAuthorizedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            OrganizerProfileResponse? profile = null;
            try
            {
                var resp = await client.GetAsync("api/OrganizerProfile/organizer-detail");
                if (resp.IsSuccessStatusCode)
                {
                    var result = await resp.Content
                        .ReadFromJsonAsync<ApiResponseModel<OrganizerProfileResponse>>();
                    profile = result?.Data;
                }
            }
            catch { /* chưa có hồ sơ */ }

            if (profile == null)
                return RedirectToAction("RegisterOrganizer");

            return View(profile);
        }
    }
}
