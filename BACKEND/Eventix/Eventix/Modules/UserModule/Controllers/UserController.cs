using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Controllers;
using Eventix.Modules.UserModule.DTOs;
using Eventix.Modules.UserModule.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eventix.Modules.UserModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : BaseApiController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponseModel<UserResponse>>> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.GetUserByIdAsync(userId);
            return SuccessResponse(SystemSuccess.USER_RETRIEVED, response);
        }

        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseModel<UserResponse>>> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.UpdateProfileAsync(userId, request);
            return SuccessResponse(SystemSuccess.USER_UPDATED, response);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponseModel<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.ChangePasswordAsync(userId, request);
            return SuccessResponse(SystemSuccess.PASSWORD_RESET_SUCCESS);
        }
    }
}
