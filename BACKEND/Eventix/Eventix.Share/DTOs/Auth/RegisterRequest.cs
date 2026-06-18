namespace Eventix.Share.Auth
{
    public class RegisterRequest
    {
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
