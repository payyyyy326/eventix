using Eventix.Entities;

namespace Eventix.Modules.UserModule.DTOs
{
    public class UserResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool EmailVerified { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }
        public List<string> Roles { get; set; } = new();

    }
}
