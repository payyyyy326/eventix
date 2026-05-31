namespace Eventix.Modules.UserModule.DTOs
{
    public class UserUpdateRequest
    {
        public string FullName { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

    }
}
