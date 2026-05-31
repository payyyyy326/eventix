using Eventix.Modules.Auth.DTOs;

namespace Eventix.Modules.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthUserDto> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request);
        Task ResendOtpAsync(ResendOtpRequest request);
        Task LogoutAsync(Guid userId);
    }
}
