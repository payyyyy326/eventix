using Eventix.Common.Constants.SystemData;
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
                .ReadFromJsonAsync<ApiResponseModel<AuthResponse>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? "Đăng nhập thất bại");
                return View(model);
            }

            if (result.Data != null)
            {
                Response.Cookies.Append("token", result.Data.Token);
                Response.Cookies.Append("refreshToken", result.Data.RefreshToken);
                Response.Cookies.Append("userName", result.Data.User.FullName ?? result.Data.User.Email);
                Response.Cookies.Append("avatarUrl", result.Data.User.AvatarUrl ?? "/images/default-avatar.png");
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgetPasswordRequest());
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync("api/auth/forgot-password", model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError("", result?.Message ?? SystemError.EMAIL_NOT_FOUND.Message);
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("VerifyResetOtp", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string otpCode)
        {
            return View(new ResetPasswordRequest
            {
                Email = email,
                OtpCode = otpCode
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync(
                "api/auth/reset-password",
                model);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                ModelState.AddModelError(
                    "",
                    result?.Message ?? SystemError.INVALID_OTP.Message);

                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = SystemError.EMAIL_REQUIRED.Message;
                return RedirectToAction("Register");
            }

            var client = _httpClientFactory.CreateClient("Eventix");

            var response = await client.PostAsJsonAsync(
                "api/auth/resend-otp",
                new ResendOtpRequest
                {
                    Email = email
                });

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseModel<object>>();

            if (!response.IsSuccessStatusCode || result == null || !result.IsSuccess)
            {
                TempData["Error"] = result?.Message ?? SystemError.BAD_REQUEST.Message;

                return RedirectToAction("VerifyOtp", new { email });
            }

            TempData["Success"] = result.Message;

            return RedirectToAction("VerifyOtp", new { email });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var client = _httpClientFactory.CreateClient("Eventix");

            var accessToken = Request.Cookies["token"];
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await client.PostAsJsonAsync(
                    "api/auth/logout",
                    new LogoutRequest
                    {
                        RefreshToken = refreshToken
                    });
            }

            Response.Cookies.Delete("token");
            Response.Cookies.Delete("refreshToken");
            Response.Cookies.Delete("userName");
            Response.Cookies.Delete("avatarUrl");

            TempData["Success"] = SystemSuccess.LOGOUT_SUCCESS.Message;

            return RedirectToAction("Login");
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
                .ReadFromJsonAsync<ApiResponseModel<AuthUserDto>>();

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

            var verifyResponse = await client.PostAsJsonAsync(
                "api/auth/verify-otp",
                model);

            var verifyResult = await verifyResponse.Content
                .ReadFromJsonAsync<ApiResponseModel<AuthResponse>>();

            if (!verifyResponse.IsSuccessStatusCode ||
                verifyResult == null ||
                !verifyResult.IsSuccess ||
                verifyResult.Data == null)
            {
                ModelState.AddModelError(
                    "",
                    verifyResult?.Message ?? SystemError.INVALID_OTP.Message);

                return View(model);
            }

            Response.Cookies.Append("token", verifyResult.Data.Token);
            Response.Cookies.Append("refreshToken", verifyResult.Data.RefreshToken);
            Response.Cookies.Append("userName", verifyResult.Data.User.FullName ?? verifyResult.Data.User.Email);
            Response.Cookies.Append("avatarUrl", verifyResult.Data.User.AvatarUrl ?? "/images/default-avatar.png");

            TempData["Success"] = SystemSuccess.LOGIN_SUCCESS.Message;
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult VerifyResetOtp(string email)
        {
            return View(new VerifyOtpRequest
            {
                Email = email
            });
        }

        [HttpPost]
        public IActionResult VerifyResetOtp(VerifyOtpRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            return RedirectToAction("ResetPassword", new
            {
                email = model.Email,
                otpCode = model.OtpCode
            });
        }
    }
}
