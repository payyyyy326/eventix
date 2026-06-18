using Eventix.Share.Auth;

namespace Eventix.Modules.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthUserDto> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request);
        Task ResendOtpAsync(ResendOtpRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(LogoutRequest request);
        Task ForgotPasswordAsync(ForgetPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}
