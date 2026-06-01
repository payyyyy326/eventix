namespace Eventix.Modules.UserModule.DTOs
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public IFormFile? Avatar { get; set; }
    }
}
