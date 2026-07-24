using System.ComponentModel.DataAnnotations;

namespace Eventix.Share.Organizer
{
    public class UpdateOrganizerProfileRequest
    {
        [Required(ErrorMessage = "Organization name is required.")]
        [StringLength(
            150,
            ErrorMessage = "Organization name cannot exceed 150 characters.")]
        public string OrganizationName { get; set; } = string.Empty;

        [StringLength(
            1000,
            ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [EmailAddress(ErrorMessage = "Invalid contact email.")]
        [StringLength(
            150,
            ErrorMessage = "Contact email cannot exceed 150 characters.")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Invalid contact phone number.")]
        [StringLength(
            20,
            ErrorMessage = "Contact phone cannot exceed 20 characters.")]
        public string? ContactPhone { get; set; }
    }
}