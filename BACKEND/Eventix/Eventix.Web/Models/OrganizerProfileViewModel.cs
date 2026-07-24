using System.ComponentModel.DataAnnotations;

namespace Eventix.Web.Models.Organizer
{
    public class OrganizerProfileViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Organization name is required.")]
        [StringLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [EmailAddress(ErrorMessage = "Invalid contact email.")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Invalid contact phone.")]
        public string? ContactPhone { get; set; }

        public string Status { get; set; } = string.Empty;

        public Guid? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}