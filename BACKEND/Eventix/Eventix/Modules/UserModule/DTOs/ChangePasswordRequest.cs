using System.ComponentModel.DataAnnotations;

namespace Eventix.Modules.UserModule.DTOs
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = null!;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
