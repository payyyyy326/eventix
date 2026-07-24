using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Organizer
{
    public class CreateOrganizerProfileRequest
    {
        [Required]
        [StringLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? ContactEmail { get; set; }

        [Phone]
        [StringLength(20)]
        public string? ContactPhone { get; set; }
    }
}