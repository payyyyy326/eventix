namespace Eventix.Share.Admin
{
    /// <summary>
    /// Thông tin người dùng dành cho admin quản lý.
    /// </summary>
    public class AdminUserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = null!;
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();

        /// <summary>True nếu user đã nộp đơn làm organizer.</summary>
        public bool HasOrganizerProfile { get; set; }

        /// <summary>Trạng thái hồ sơ organizer (Pending/Approved/Rejected/Suspended), null nếu chưa đăng ký.</summary>
        public string? OrganizerStatus { get; set; }
    }
}
