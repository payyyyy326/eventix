using Microsoft.AspNetCore.Http;

namespace Eventix.Share.DTOs.User
{
    public class UploadAvatarRequest
    {
        public IFormFile? Avatar { get; set; }
    }
}
