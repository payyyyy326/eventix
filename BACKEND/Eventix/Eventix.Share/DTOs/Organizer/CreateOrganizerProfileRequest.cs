namespace Eventix.Share.Organizer
{
    public class CreateOrganizerProfileRequest
    {
        public string OrganizationName { get; set; } = null!;
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }
}
