using Eventix.Modules.UserModule.DTOs;

namespace Eventix.Modules.VenueModule.DTOs
{
    public class VenueResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Address { get; set; }

        public string? City { get; set; }

        public int Capacity { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public virtual UserResponse? User { get; set; }

    }
}
