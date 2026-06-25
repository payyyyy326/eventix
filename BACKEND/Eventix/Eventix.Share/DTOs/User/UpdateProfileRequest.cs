namespace Eventix.Share.User
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
}
