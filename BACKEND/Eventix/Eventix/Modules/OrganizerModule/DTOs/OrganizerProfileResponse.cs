using Eventix.Entities;

namespace Eventix.Modules.OrganizerModule.DTOs
{
    public class OrganizerProfileResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string OrganizationName { get; set; } = null!;

        public string? Description { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }

        public string Status { get; set; } = null!;

        public Guid? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User? ApprovedByNavigation { get; set; }

        public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    }
}
