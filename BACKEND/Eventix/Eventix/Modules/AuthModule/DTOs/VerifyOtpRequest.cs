namespace Eventix.Modules.Auth.DTOs
{
    public class VerifyOtpRequest
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }

    public class ResendOtpRequest
    {
        public string Email { get; set; } = null!;
    }
}
