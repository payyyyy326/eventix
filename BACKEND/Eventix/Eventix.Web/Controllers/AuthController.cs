using Eventix.Share.Auth;
using Eventix.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync("api/auth/login", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Đăng nhập thất bại");
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterRequest());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync("api/auth/register", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Đăng ký thất bại");
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }


        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpRequest
            {
                Email = email
            });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync("api/auth/verify-otp", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Xác thực OTP thất bại");
                return View(model);
            }

            TempData["Success"] = "Xác thực email thành công. Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }
    }
}
