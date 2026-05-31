using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Controllers;
using Eventix.Modules.Auth.DTOs;
using Eventix.Modules.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.Auth.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponseModel<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return SuccessResponse(SystemSuccess.LOGIN_SUCCESS, response);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponseModel<AuthUserDto>>> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            return SuccessResponse(SystemSuccess.REGISTER_PENDING_VERIFY, response);
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponseModel<AuthResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var response = await _authService.VerifyOtpAsync(request);
            return SuccessResponse(SystemSuccess.EMAIL_VERIFY_SUCCESS, response);
        }

        [AllowAnonymous]
        [HttpPost("resend-otp")]
        public async Task<ActionResult<ApiResponseModel<object>>> ResendOtp([FromBody] ResendOtpRequest request)
        {
            await _authService.ResendOtpAsync(request);
            return SuccessResponse(SystemSuccess.OTP_SEND_SUCCESS);
        }
    }
}
