using Eventix.Share.Common.Models;
using Eventix.Share.User;
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

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var client = CreateAuthorizedClient();

            if (client == null)
                return RedirectToAction("Login", "Auth");

            var response = await client.GetAsync("api/user/profile");

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

            if (!response.IsSuccessStatusCode || result?.Data == null)
                return RedirectToAction("Profile");

            var model = new UpdateProfileRequest
            {
                FullName = result.Data.FullName,
                PhoneNumber = result.Data.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UpdateProfileRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = CreateAuthorizedClient();

            if (client == null)
                return RedirectToAction("Login", "Auth");

            var response = await client.PutAsJsonAsync("api/user/profile", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<UserResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Update profile failed");
                return View(model);
            }

            if (result.Data != null)
            {
                Response.Cookies.Append("userName", result.Data.FullName ?? result.Data.Email);
                Response.Cookies.Append("avatarUrl", result.Data.AvatarUrl ?? "/images/default-avatar.png");
            }

            TempData["Success"] = result.Message;

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
    }
}