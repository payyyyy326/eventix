using Eventix.Share.Event;
using Eventix.Share.User;

namespace Eventix.Share.Organizer
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

        public virtual UserResponse? ApprovedByNavigation { get; set; }

        public virtual ICollection<EventResponse> Events { get; set; } = new List<EventResponse>();

    }
}
