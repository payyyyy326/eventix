using Eventix.Share.User;

namespace Eventix.Web.Models;

public class ProfileViewModel
{
    public UserResponse Profile { get; set; } = null!;
    public UpdateProfileRequest EditRequest { get; set; } = new();
}
